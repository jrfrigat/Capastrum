using System.Text.Json;
using Capastrum.JsonRpc;
using Capastrum.Mcp;
using Capastrum.Mcp.Client;
using Capastrum.Mcp.Protocol;
using Xunit;

namespace Capastrum.Mcp.Client.Tests;

public sealed class McpClientTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task ConnectDiscoversServer()
    {
        await using var host = await CreateHostAsync(server => server.RegisterTool(
            "echo",
            "Echoes a value",
            (_, _) => ValueTask.FromResult<McpResult>(McpToolResult.Text("ok"))));

        Assert.Equal("test", host.Client.ServerInfo?.Name);
        Assert.Contains(McpProtocol.Version, host.Client.SupportedVersions);
        Assert.NotNull(host.Client.Capabilities?.Tools);
    }

    [Fact]
    public async Task ListsToolsAndCallsTool()
    {
        await using var host = await CreateHostAsync(server => server.RegisterTool(
            "echo",
            "Echoes a value",
            (context, _) => ValueTask.FromResult<McpResult>(
                McpToolResult.Text(context.Arguments.GetProperty("value").GetString()!))));

        var tools = await host.Client.ListToolsAsync();
        Assert.Contains(tools.Tools, static tool => tool.Name == "echo");

        var result = await host.Client.CallToolAsync(
            "echo",
            JsonSerializer.SerializeToElement(new { value = "hi" }));

        Assert.Equal("hi", Assert.IsType<McpTextContent>(result.Content[0]).Text);
    }

    [Fact]
    public async Task CallToolResolvesSamplingInput()
    {
        var samplingCalls = 0;

        await using var host = await CreateHostAsync(
            server => server.RegisterTool(
                "sample",
                "Samples",
                (context, _) =>
                {
                    if (context.InputResponses is { Count: > 0 })
                    {
                        return ValueTask.FromResult<McpResult>(McpToolResult.Text("sampled"));
                    }

                    return ValueTask.FromResult<McpResult>(McpToolResult.InputRequired(
                        "sample-1",
                        new McpCreateMessageInputRequest
                        {
                            Params = new McpCreateMessageRequestParams
                            {
                                MaxTokens = 5,
                                Messages = new[]
                                {
                                    new McpSamplingMessage
                                    {
                                        Role = McpRole.User,
                                        Content = JsonSerializer.SerializeToElement(new { type = "text", text = "hi" }),
                                    },
                                },
                            },
                        },
                        "state"));
                }),
            new McpClientOptions
            {
                SamplingHandler = (_, _) =>
                {
                    samplingCalls++;
                    return ValueTask.FromResult(new McpCreateMessageResult
                    {
                        Content = JsonSerializer.SerializeToElement(new { type = "text", text = "sample" }),
                        Model = "test",
                        Role = McpRole.Assistant,
                    });
                },
            });

        var result = await host.Client.CallToolAsync("sample");

        Assert.Equal("sampled", Assert.IsType<McpTextContent>(result.Content[0]).Text);
        Assert.Equal(1, samplingCalls);
    }

    [Fact]
    public async Task SubscriptionRaisesToolsChanged()
    {
        await using var host = await CreateHostAsync();
        var changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        host.Client.ToolsListChanged += () => changed.TrySetResult(true);

        await host.Client.ListenAsync(new McpSubscriptionFilter { ToolsListChanged = true });
        await host.Server.NotifyToolsChangedAsync();

        Assert.True(await changed.Task.WaitAsync(Timeout));
    }

    private static async Task<TestHost> CreateHostAsync(
        Action<McpServer>? configure = null,
        McpClientOptions? options = null)
    {
        var server = new McpServer(new McpServerOptions { Name = "test", Version = "1.0.0" });
        configure?.Invoke(server);

        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        var serverPeer = new JsonRpcPeer(serverTransport);
        var session = server.CreateSession(
            (notification, cancellationToken) => serverPeer.NotifyAsync(
                notification.Method,
                notification.Params,
                cancellationToken));

        serverPeer.RequestHandler = session.HandleRequestAsync;
        serverPeer.NotificationReceived += session.HandleNotification;

        var client = await McpClient.ConnectAsync(clientTransport, options ?? new McpClientOptions());

        return new TestHost(server, serverPeer, session, client);
    }

    private sealed class TestHost(
        McpServer server,
        JsonRpcPeer serverPeer,
        McpServerSession session,
        McpClient client) : IAsyncDisposable
    {
        public McpServer Server { get; } = server;
        public McpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            await session.DisposeAsync();
            await serverPeer.DisposeAsync();
        }
    }
}

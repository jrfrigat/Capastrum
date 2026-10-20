using System.Text.Json;
using Capastrum.JsonRpc;
using Capastrum.JsonRpc.Serialization;
using Capastrum.Mcp;
using Capastrum.Mcp.Protocol;
using Capastrum.Mcp.Protocol.Serialization;
using Xunit;

namespace Capastrum.Mcp.Tests;

public sealed class McpServerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly string[] SupportedVersions = [McpProtocol.Version];
    private static readonly string[] CompletionValues = ["alpha", "beta"];

    [Fact]
    public async Task DiscoverReturnsCapabilities()
    {
        await using var connection = CreateConnection();
        connection.Server.RegisterTool(
            "echo",
            "Echoes a value",
            (_, _) => ValueTask.FromResult<McpResult>(McpToolResult.Text("ok")));

        var parameters = new McpDiscoverRequestParams { Meta = CreateMeta() };
        var result = await connection.Client
            .InvokeAsync(
                McpProtocol.Methods.Discover,
                JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpDiscoverRequestParams))
            .AsTask()
            .WaitAsync(Timeout);

        var discover = result.Deserialize(McpJsonContext.Default.McpDiscoverResult);

        Assert.Equal(SupportedVersions, discover!.SupportedVersions);
        Assert.NotNull(discover.Capabilities.Tools);
        Assert.Equal("test", discover.Meta?.ServerInfo?.Name);
    }

    [Fact]
    public async Task ToolsListAndCall()
    {
        await using var connection = CreateConnection();
        connection.Server.RegisterTool(
            "echo",
            "Echoes a value",
            (context, _) => ValueTask.FromResult<McpResult>(
                McpToolResult.Text(context.Arguments.GetProperty("value").GetString()!)));

        var listParameters = new McpListToolsRequestParams { Meta = CreateMeta() };
        var listResult = await connection.Client
            .InvokeAsync(
                McpProtocol.Methods.ListTools,
                JsonSerializer.SerializeToElement(listParameters, McpJsonContext.Default.McpListToolsRequestParams))
            .AsTask()
            .WaitAsync(Timeout);

        var tools = listResult.Deserialize(McpJsonContext.Default.McpListToolsResult);
        Assert.Contains(tools!.Tools, static tool => tool.Name == "echo");

        var callParameters = new McpCallToolRequestParams
        {
            Meta = CreateMeta(),
            Name = "echo",
            Arguments = JsonSerializer.SerializeToElement(new { value = "hello" }),
        };

        var callResult = await connection.Client
            .InvokeAsync(
                McpProtocol.Methods.CallTool,
                JsonSerializer.SerializeToElement(callParameters, McpJsonContext.Default.McpCallToolRequestParams))
            .AsTask()
            .WaitAsync(Timeout);

        var call = callResult.Deserialize(McpJsonContext.Default.McpCallToolResult);
        Assert.Equal("hello", Assert.IsType<McpTextContent>(call!.Content[0]).Text);
    }

    [Fact]
    public async Task ResourceAndPrompt()
    {
        await using var connection = CreateConnection();
        connection.Server.RegisterResource(
            new McpResource { Uri = "file:///a.txt", Name = "a" },
            (context, _) => ValueTask.FromResult(new McpReadResourceResult
            {
                Contents = new[]
                {
                    new McpResourceContents { Uri = context.Uri, Text = "body" },
                },
            }));

        connection.Server.RegisterPrompt(
            new McpPrompt { Name = "greet" },
            (_, _) => ValueTask.FromResult(new McpGetPromptResult
            {
                Messages = new[]
                {
                    new McpPromptMessage
                    {
                        Role = McpRole.User,
                        Content = new McpTextContent { Text = "hi" },
                    },
                },
            }));

        var readParameters = new McpReadResourceRequestParams
        {
            Meta = CreateMeta(),
            Uri = "file:///a.txt",
        };

        var readResult = await connection.Client
            .InvokeAsync(
                McpProtocol.Methods.ReadResource,
                JsonSerializer.SerializeToElement(readParameters, McpJsonContext.Default.McpReadResourceRequestParams))
            .AsTask()
            .WaitAsync(Timeout);

        var read = readResult.Deserialize(McpJsonContext.Default.McpReadResourceResult);
        Assert.Equal("body", read!.Contents[0].Text);

        var promptParameters = new McpGetPromptRequestParams
        {
            Meta = CreateMeta(),
            Name = "greet",
        };

        var promptResult = await connection.Client
            .InvokeAsync(
                McpProtocol.Methods.GetPrompt,
                JsonSerializer.SerializeToElement(promptParameters, McpJsonContext.Default.McpGetPromptRequestParams))
            .AsTask()
            .WaitAsync(Timeout);

        var prompt = promptResult.Deserialize(McpJsonContext.Default.McpGetPromptResult);
        Assert.Equal("hi", Assert.IsType<McpTextContent>(prompt!.Messages[0].Content).Text);
    }

    [Fact]
    public async Task CompletionReturnsValues()
    {
        await using var connection = CreateConnection();
        connection.Server.SetCompletionHandler((_, _) => ValueTask.FromResult(new McpCompleteResult
        {
            Completion = new McpCompletion { Values = CompletionValues },
        }));

        var parameters = new McpCompleteRequestParams
        {
            Meta = CreateMeta(),
            Ref = new McpPromptReference { Name = "greet" },
            Argument = new McpCompletionArgument { Name = "name", Value = "a" },
        };

        var result = await connection.Client
            .InvokeAsync(
                McpProtocol.Methods.Complete,
                JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpCompleteRequestParams))
            .AsTask()
            .WaitAsync(Timeout);

        var completion = result.Deserialize(McpJsonContext.Default.McpCompleteResult);
        Assert.Equal(CompletionValues, completion!.Completion.Values);
    }

    [Fact]
    public async Task SubscriptionReceivesToolListChanged()
    {
        await using var connection = CreateConnection();
        var notificationReceived = new TaskCompletionSource<JsonRpcNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        connection.Client.NotificationReceived += notification =>
        {
            if (notification.Method == McpProtocol.Notifications.ToolsListChanged)
            {
                notificationReceived.TrySetResult(notification);
            }
        };

        var parameters = new McpSubscriptionsListenRequestParams
        {
            Meta = CreateMeta(),
            Notifications = new McpSubscriptionFilter { ToolsListChanged = true },
        };

        var result = await connection.Client
            .InvokeAsync(
                McpProtocol.Methods.Listen,
                JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpSubscriptionsListenRequestParams))
            .AsTask()
            .WaitAsync(Timeout);

        var listen = result.Deserialize(McpJsonContext.Default.McpSubscriptionsListenResult);
        Assert.NotNull(listen);

        await connection.Server.NotifyToolsChangedAsync();

        var notification = await notificationReceived.Task.WaitAsync(Timeout);
        Assert.True(notification.Params?.GetProperty("_meta").TryGetProperty(
            McpProtocol.MetaKeys.SubscriptionId,
            out _));
    }

    [Fact]
    public async Task UnsupportedProtocolVersionIsRejected()
    {
        await using var connection = CreateConnection();
        var parameters = new McpDiscoverRequestParams { Meta = CreateMeta("2020-01-01") };

        var exception = await Assert.ThrowsAsync<JsonRpcException>(() => connection.Client
            .InvokeAsync(
                McpProtocol.Methods.Discover,
                JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpDiscoverRequestParams))
            .AsTask()
            .WaitAsync(Timeout));

        Assert.Equal(McpErrorCode.UnsupportedProtocolVersion, exception.Code);
    }

    [Fact]
    public async Task HttpHandlerValidatesMethodHeader()
    {
        var server = new McpServer(new McpServerOptions { Name = "http", Version = "1.0.0" });
        var parameters = new McpDiscoverRequestParams { Meta = CreateMeta() };
        var request = new JsonRpcRequest
        {
            Id = new JsonRpcId(1),
            Method = McpProtocol.Methods.Discover,
            Params = JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpDiscoverRequestParams),
        };
        var body = JsonSerializer.SerializeToUtf8Bytes<JsonRpcMessage>(request, JsonRpcJsonContext.Default.JsonRpcMessage);

        var response = await server.HandleHttpRequestAsync(new McpHttpRequest
        {
            Method = "POST",
            Headers = new Dictionary<string, string>
            {
                [McpProtocol.Headers.Method] = McpProtocol.Methods.Discover,
            },
            Body = body,
        });

        Assert.Equal(200, response.StatusCode);

        var missingHeader = await server.HandleHttpRequestAsync(new McpHttpRequest
        {
            Method = "POST",
            Headers = new Dictionary<string, string>(),
            Body = body,
        });

        Assert.Equal(400, missingHeader.StatusCode);
    }

    private static Connection CreateConnection()
    {
        var server = new McpServer(new McpServerOptions { Name = "test", Version = "1.0.0" });
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        var client = new JsonRpcPeer(clientTransport);
        var serverPeer = new JsonRpcPeer(serverTransport);
        var session = server.CreateSession(
            (notification, cancellationToken) => serverPeer.NotifyAsync(
                notification.Method,
                notification.Params,
                cancellationToken));

        serverPeer.RequestHandler = session.HandleRequestAsync;
        serverPeer.NotificationReceived += session.HandleNotification;

        return new Connection
        {
            Server = server,
            Client = client,
            ServerPeer = serverPeer,
            Session = session,
        };
    }

    private static McpRequestMeta CreateMeta(string version = McpProtocol.Version) => new()
    {
        ProtocolVersion = version,
        ClientCapabilities = new McpClientCapabilities(),
    };

    private sealed class Connection : IAsyncDisposable
    {
        public required McpServer Server { get; init; }
        public required JsonRpcPeer Client { get; init; }
        public required JsonRpcPeer ServerPeer { get; init; }
        public required McpServerSession Session { get; init; }

        public async ValueTask DisposeAsync()
        {
            await Session.DisposeAsync();
            await ServerPeer.DisposeAsync();
            await Client.DisposeAsync();
        }
    }
}

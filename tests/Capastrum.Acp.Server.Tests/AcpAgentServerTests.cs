using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Capastrum.Acp.Protocol;
using Capastrum.Acp.Protocol.Serialization;
using Capastrum.Acp.Server;
using Capastrum.JsonRpc;
using Xunit;

namespace Capastrum.Acp.Server.Tests;

public sealed class AcpAgentServerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task InitializeReturnsAgentInfo()
    {
        await using var host = await CreateHostAsync();
        var request = new AcpInitializeRequest { ProtocolVersion = AcpProtocol.Version };

        var result = await host.HostPeer.InvokeAsync(
            AcpProtocol.Methods.Initialize,
            JsonSerializer.SerializeToElement(request, AcpJsonContext.Default.AcpInitializeRequest));

        var response = result.Deserialize(AcpJsonContext.Default.AcpInitializeResponse);

        Assert.Equal(AcpProtocol.Version, response!.ProtocolVersion);
        Assert.Equal("agent", response.AgentInfo?.Name);
    }

    [Fact]
    public async Task PromptSendsUpdateAndRequestsPermission()
    {
        string? selectedOption = null;
        AcpSessionNotification? update = null;

        await using var host = await CreateHostAsync(new AcpAgentServerOptions
        {
            AgentInfo = new AcpImplementation { Name = "agent", Version = "1.0.0" },
            NewSessionHandler = (_, _) => ValueTask.FromResult(new AcpNewSessionResponse { SessionId = "session-1" }),
            PromptHandler = async (context, cancellationToken) =>
            {
                await context.SendUpdateAsync(
                    new AcpAgentMessageChunk
                    {
                        Content = new AcpTextContent { Text = "working" },
                    },
                    cancellationToken);

                var permission = await context.RequestPermissionAsync(
                    new AcpToolCallUpdate { ToolCallId = "tool-1", Title = "Run command" },
                    new[]
                    {
                        new AcpPermissionOption
                        {
                            OptionId = "allow",
                            Name = "Allow",
                            Kind = AcpPermissionOptionKind.AllowOnce,
                        },
                    },
                    cancellationToken);

                selectedOption = (permission.Outcome as AcpSelectedPermissionOutcome)?.OptionId;

                return new AcpPromptResponse { StopReason = AcpStopReason.EndTurn };
            },
        });

        host.HostPeer.RequestHandler = (request, _) => request.Method == AcpProtocol.Methods.SessionRequestPermission
            ? ValueTask.FromResult(Success(
                request.Id,
                new AcpRequestPermissionResponse
                {
                    Outcome = new AcpSelectedPermissionOutcome { OptionId = "allow" },
                },
                AcpJsonContext.Default.AcpRequestPermissionResponse))
            : ValueTask.FromResult(Error(request.Id, JsonRpcErrorCode.MethodNotFound, "not handled"));

        host.HostPeer.NotificationReceived += notification =>
        {
            if (notification.Method == AcpProtocol.Notifications.SessionUpdate && notification.Params is { } parameters)
            {
                update = parameters.Deserialize(AcpJsonContext.Default.AcpSessionNotification);
            }
        };

        var prompt = new AcpPromptRequest
        {
            SessionId = "session-1",
            Prompt = new[] { new AcpTextContent { Text = "go" } },
        };

        var result = await host.HostPeer.InvokeAsync(
            AcpProtocol.Methods.SessionPrompt,
            JsonSerializer.SerializeToElement(prompt, AcpJsonContext.Default.AcpPromptRequest));

        var response = result.Deserialize(AcpJsonContext.Default.AcpPromptResponse);

        Assert.Equal(AcpStopReason.EndTurn, response!.StopReason);
        Assert.Equal("working", update?.Text);
        Assert.Equal("allow", selectedOption);
    }

    [Fact]
    public async Task SessionCancelStopsPrompt()
    {
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var host = await CreateHostAsync(new AcpAgentServerOptions
        {
            AgentInfo = new AcpImplementation { Name = "agent", Version = "1.0.0" },
            NewSessionHandler = (_, _) => ValueTask.FromResult(new AcpNewSessionResponse { SessionId = "session-1" }),
            PromptHandler = async (_, cancellationToken) =>
            {
                ready.TrySetResult(true);
                await Task.Delay(Timeout, cancellationToken);
                return new AcpPromptResponse { StopReason = AcpStopReason.EndTurn };
            },
        });

        var prompt = new AcpPromptRequest
        {
            SessionId = "session-1",
            Prompt = new[] { new AcpTextContent { Text = "go" } },
        };

        var promptTask = host.HostPeer
            .InvokeAsync(
                AcpProtocol.Methods.SessionPrompt,
                JsonSerializer.SerializeToElement(prompt, AcpJsonContext.Default.AcpPromptRequest))
            .AsTask();

        await ready.Task.WaitAsync(Timeout);

        await host.HostPeer.NotifyAsync(
            AcpProtocol.Notifications.SessionCancel,
            JsonSerializer.SerializeToElement(
                new AcpCancelNotification { SessionId = "session-1" },
                AcpJsonContext.Default.AcpCancelNotification));

        var result = await promptTask.WaitAsync(Timeout);
        var response = result.Deserialize(AcpJsonContext.Default.AcpPromptResponse);

        Assert.Equal(AcpStopReason.Cancelled, response!.StopReason);
    }

    [Fact]
    public async Task PromptReadsFileThroughClient()
    {
        string? content = null;

        await using var host = await CreateHostAsync(new AcpAgentServerOptions
        {
            AgentInfo = new AcpImplementation { Name = "agent", Version = "1.0.0" },
            NewSessionHandler = (_, _) => ValueTask.FromResult(new AcpNewSessionResponse { SessionId = "session-1" }),
            PromptHandler = async (context, cancellationToken) =>
            {
                var read = await context.ReadTextFileAsync(
                    new AcpReadTextFileRequest
                    {
                        SessionId = context.SessionId,
                        Path = "/tmp/a.txt",
                    },
                    cancellationToken);

                content = read.Content;

                return new AcpPromptResponse { StopReason = AcpStopReason.EndTurn };
            },
        });

        host.HostPeer.RequestHandler = (request, _) => request.Method == AcpProtocol.Methods.FsReadTextFile
            ? ValueTask.FromResult(Success(
                request.Id,
                new AcpReadTextFileResponse { Content = "file" },
                AcpJsonContext.Default.AcpReadTextFileResponse))
            : ValueTask.FromResult(Error(request.Id, JsonRpcErrorCode.MethodNotFound, "not handled"));

        var prompt = new AcpPromptRequest
        {
            SessionId = "session-1",
            Prompt = new[] { new AcpTextContent { Text = "go" } },
        };

        await host.HostPeer.InvokeAsync(
            AcpProtocol.Methods.SessionPrompt,
            JsonSerializer.SerializeToElement(prompt, AcpJsonContext.Default.AcpPromptRequest));

        Assert.Equal("file", content);
    }

    private static async Task<TestHost> CreateHostAsync(AcpAgentServerOptions? options = null)
    {
        var serverOptions = options ?? new AcpAgentServerOptions
        {
            AgentInfo = new AcpImplementation { Name = "agent", Version = "1.0.0" },
            NewSessionHandler = (_, _) => ValueTask.FromResult(new AcpNewSessionResponse { SessionId = "session-1" }),
            PromptHandler = (_, _) => ValueTask.FromResult(new AcpPromptResponse
            {
                StopReason = AcpStopReason.EndTurn,
            }),
        };

        var server = new AcpAgentServer(serverOptions);
        var (hostTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        var hostPeer = new JsonRpcPeer(hostTransport);
        var serverPeer = new JsonRpcPeer(serverTransport);
        var session = server.CreateSession(serverPeer);

        serverPeer.RequestHandler = session.HandleRequestAsync;
        serverPeer.NotificationReceived += session.HandleNotification;

        await Task.CompletedTask;
        return new TestHost(hostPeer, serverPeer, session);
    }

    private static JsonRpcResponse Success<T>(JsonRpcId id, T result, JsonTypeInfo<T> typeInfo) => new()
    {
        Id = id,
        Result = JsonSerializer.SerializeToElement(result, typeInfo),
    };

    private static JsonRpcResponse Error(JsonRpcId id, int code, string message) => new()
    {
        Id = id,
        Error = new JsonRpcError { Code = code, Message = message },
    };

    private sealed class TestHost(
        JsonRpcPeer hostPeer,
        JsonRpcPeer serverPeer,
        AcpAgentSession session) : IAsyncDisposable
    {
        public JsonRpcPeer HostPeer { get; } = hostPeer;

        public async ValueTask DisposeAsync()
        {
            await HostPeer.DisposeAsync();
            await session.DisposeAsync();
            await serverPeer.DisposeAsync();
        }
    }
}

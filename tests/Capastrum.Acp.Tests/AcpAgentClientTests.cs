using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Capastrum.Acp;
using Capastrum.Acp.Protocol;
using Capastrum.Acp.Protocol.Serialization;
using Capastrum.JsonRpc;
using Xunit;

namespace Capastrum.Acp.Tests;

public sealed class AcpAgentClientTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task ConnectAndCreateSession()
    {
        var (host, agentPeer) = await CreateHostAsync();
        await using var hostLifetime = host;
        await using var agentLifetime = agentPeer;

        Assert.Equal("fake", host.AgentInfo?.Name);

        var session = await host.NewSessionAsync("/tmp");
        Assert.Equal("session-1", session.SessionId);
    }

    [Fact]
    public async Task PromptReceivesUpdatesAndAllowedPermission()
    {
        var (selectedOption, response, update) = await RunPromptWithPermissionAsync(allow: true);

        Assert.Equal(AcpStopReason.EndTurn, response.StopReason);
        Assert.Equal("hello", update?.Text);
        Assert.Equal("allow", selectedOption);
    }

    [Fact]
    public async Task PromptReceivesDeniedPermission()
    {
        var (selectedOption, response, _) = await RunPromptWithPermissionAsync(allow: false);

        Assert.Equal(AcpStopReason.EndTurn, response.StopReason);
        Assert.Equal("deny", selectedOption);
    }

    [Fact]
    public async Task CancelSendsSessionCancelNotification()
    {
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var (host, agentPeer) = await CreateHostAsync(async (agent, request, cancellationToken) =>
        {
            agent.NotificationReceived += notification =>
            {
                if (notification.Method == AcpProtocol.Notifications.SessionCancel)
                {
                    cancelled.TrySetResult(true);
                }
            };

            ready.TrySetResult(true);
            await cancelled.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            return Success(
                request.Id,
                new AcpPromptResponse { StopReason = AcpStopReason.Cancelled },
                AcpJsonContext.Default.AcpPromptResponse);
        });

        await using var hostLifetime = host;
        await using var agentLifetime = agentPeer;

        var promptTask = host.SendPromptAsync("session-1", new[] { new AcpTextContent { Text = "go" } }).AsTask();
        await ready.Task.WaitAsync(Timeout);
        await host.CancelAsync("session-1");

        var response = await promptTask.WaitAsync(Timeout);

        Assert.Equal(AcpStopReason.Cancelled, response.StopReason);
    }

    [Fact]
    public async Task FileReadIsHandledByHost()
    {
        string? content = null;

        var (host, agentPeer) = await CreateHostAsync(async (agent, request, cancellationToken) =>
        {
            var prompt = request.Params!.Value.Deserialize(AcpJsonContext.Default.AcpPromptRequest)!;
            var readRequest = new AcpReadTextFileRequest
            {
                SessionId = prompt.SessionId,
                Path = "/tmp/a.txt",
            };

            var readResult = await agent.InvokeAsync(
                AcpProtocol.Methods.FsReadTextFile,
                JsonSerializer.SerializeToElement(readRequest, AcpJsonContext.Default.AcpReadTextFileRequest),
                cancellationToken).ConfigureAwait(false);

            content = readResult.Deserialize(AcpJsonContext.Default.AcpReadTextFileResponse)?.Content;

            return Success(
                request.Id,
                new AcpPromptResponse { StopReason = AcpStopReason.EndTurn },
                AcpJsonContext.Default.AcpPromptResponse);
        });

        await using var hostLifetime = host;
        await using var agentLifetime = agentPeer;

        host.OnReadTextFile((_, _) => ValueTask.FromResult(new AcpReadTextFileResponse { Content = "file" }));

        await host.SendPromptAsync("session-1", new[] { new AcpTextContent { Text = "go" } });

        Assert.Equal("file", content);
    }

    private static async Task<(string? SelectedOption, AcpPromptResponse Response, AcpSessionNotification? Update)>
        RunPromptWithPermissionAsync(bool allow)
    {
        string? selectedOption = null;
        AcpSessionNotification? receivedUpdate = null;

        var (host, agentPeer) = await CreateHostAsync(async (agent, request, cancellationToken) =>
        {
            var prompt = request.Params!.Value.Deserialize(AcpJsonContext.Default.AcpPromptRequest)!;
            var update = new AcpSessionNotification
            {
                SessionId = prompt.SessionId,
                Update = new AcpAgentMessageChunk
                {
                    Content = new AcpTextContent { Text = "hello" },
                },
            };

            await agent.NotifyAsync(
                AcpProtocol.Notifications.SessionUpdate,
                JsonSerializer.SerializeToElement(update, AcpJsonContext.Default.AcpSessionNotification),
                cancellationToken).ConfigureAwait(false);

            var permission = new AcpRequestPermissionRequest
            {
                SessionId = prompt.SessionId,
                ToolCall = new AcpToolCallUpdate
                {
                    ToolCallId = "tool-1",
                    Title = "Run command",
                    RawInput = JsonSerializer.SerializeToElement(new { command = "echo hi" }),
                },
                Options = new[]
                {
                    new AcpPermissionOption
                    {
                        OptionId = "allow",
                        Name = "Allow",
                        Kind = AcpPermissionOptionKind.AllowOnce,
                    },
                    new AcpPermissionOption
                    {
                        OptionId = "deny",
                        Name = "Deny",
                        Kind = AcpPermissionOptionKind.RejectOnce,
                    },
                },
            };

            var permissionResult = await agent.InvokeAsync(
                AcpProtocol.Methods.SessionRequestPermission,
                JsonSerializer.SerializeToElement(permission, AcpJsonContext.Default.AcpRequestPermissionRequest),
                cancellationToken).ConfigureAwait(false);

            var permissionResponse = permissionResult.Deserialize(
                AcpJsonContext.Default.AcpRequestPermissionResponse);

            selectedOption = (permissionResponse?.Outcome as AcpSelectedPermissionOutcome)?.OptionId;

            return Success(
                request.Id,
                new AcpPromptResponse { StopReason = AcpStopReason.EndTurn },
                AcpJsonContext.Default.AcpPromptResponse);
        });

        await using var hostLifetime = host;
        await using var agentLifetime = agentPeer;

        host.OnActionConfirm((permission, _) => ValueTask.FromResult(
            permission.Command == "echo hi" && allow
                ? ConfirmationResult.Allowed
                : ConfirmationResult.Denied));
        host.OnSessionUpdate(update => receivedUpdate = update);

        var response = await host.SendPromptAsync(
            "session-1",
            new[] { new AcpTextContent { Text = "go" } });

        return (selectedOption, response, receivedUpdate);
    }

    private static async Task<(AcpAgentClient Host, JsonRpcPeer AgentPeer)> CreateHostAsync(
        Func<JsonRpcPeer, JsonRpcRequest, CancellationToken, ValueTask<JsonRpcResponse>>? promptHandler = null)
    {
        var (hostTransport, agentTransport) = InMemoryJsonRpcTransport.CreatePair();
        var agentPeer = new JsonRpcPeer(agentTransport);

        agentPeer.RequestHandler = (request, cancellationToken) => request.Method switch
        {
            AcpProtocol.Methods.Initialize => ValueTask.FromResult(Success(
                request.Id,
                new AcpInitializeResponse
                {
                    ProtocolVersion = AcpProtocol.Version,
                    AgentInfo = new AcpImplementation { Name = "fake", Version = "1.0.0" },
                    AgentCapabilities = new AcpAgentCapabilities(),
                },
                AcpJsonContext.Default.AcpInitializeResponse)),
            AcpProtocol.Methods.SessionNew => ValueTask.FromResult(Success(
                request.Id,
                new AcpNewSessionResponse { SessionId = "session-1" },
                AcpJsonContext.Default.AcpNewSessionResponse)),
            AcpProtocol.Methods.SessionPrompt when promptHandler is not null => promptHandler(
                agentPeer,
                request,
                cancellationToken),
            _ => ValueTask.FromResult(new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError
                {
                    Code = JsonRpcErrorCode.MethodNotFound,
                    Message = $"Method '{request.Method}' is not handled by the fake agent.",
                },
            }),
        };

        var host = await AcpAgentClient.ConnectAsync(
            hostTransport,
            new AcpClientOptions { WorkspaceRoot = Path.GetTempPath() });

        return (host, agentPeer);
    }

    private static JsonRpcResponse Success<T>(JsonRpcId id, T result, JsonTypeInfo<T> typeInfo) => new()
    {
        Id = id,
        Result = JsonSerializer.SerializeToElement(result, typeInfo),
    };
}

using System.Text.Json;
using Capastrum.Acp.Protocol;
using Capastrum.Acp.Protocol.Serialization;
using Capastrum.JsonRpc;

namespace Capastrum.Acp.Server;

/// <summary>
/// The context passed to an ACP prompt handler.
/// </summary>
public sealed class AcpPromptContext
{
    private readonly AcpAgentSession _session;

    internal AcpPromptContext(
        AcpAgentSession session,
        JsonRpcId requestId,
        AcpPromptRequest request,
        CancellationToken cancellationToken)
    {
        _session = session;
        RequestId = requestId;
        SessionId = request.SessionId;
        Prompt = request.Prompt;
        Meta = request.Meta;
        CancellationToken = cancellationToken;
    }

    /// <summary>The JSON-RPC id of the prompt request.</summary>
    public JsonRpcId RequestId { get; }

    /// <summary>The session identifier.</summary>
    public string SessionId { get; }

    /// <summary>The prompt content.</summary>
    public IReadOnlyList<AcpContentBlock> Prompt { get; }

    /// <summary>Implementation-specific metadata.</summary>
    public IReadOnlyDictionary<string, JsonElement>? Meta { get; }

    /// <summary>Cancelled when the client sends <c>session/cancel</c>.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Sends a session update to the client.</summary>
    public ValueTask SendUpdateAsync(AcpSessionUpdate update, CancellationToken cancellationToken = default) =>
        _session.SendUpdateAsync(SessionId, update, cancellationToken);

    /// <summary>Requests permission from the client.</summary>
    public ValueTask<AcpRequestPermissionResponse> RequestPermissionAsync(
        AcpRequestPermissionRequest request,
        CancellationToken cancellationToken = default) =>
        _session.RequestPermissionAsync(request, cancellationToken);

    /// <summary>Requests permission with this context's session id.</summary>
    public ValueTask<AcpRequestPermissionResponse> RequestPermissionAsync(
        AcpToolCallUpdate toolCall,
        IReadOnlyList<AcpPermissionOption> options,
        CancellationToken cancellationToken = default) =>
        RequestPermissionAsync(
            new AcpRequestPermissionRequest
            {
                SessionId = SessionId,
                ToolCall = toolCall,
                Options = options,
            },
            cancellationToken);

    /// <summary>Reads a text file through the client.</summary>
    public ValueTask<AcpReadTextFileResponse> ReadTextFileAsync(
        AcpReadTextFileRequest request,
        CancellationToken cancellationToken = default) =>
        _session.InvokeClientAsync(
            AcpProtocol.Methods.FsReadTextFile,
            request,
            AcpJsonContext.Default.AcpReadTextFileRequest,
            AcpJsonContext.Default.AcpReadTextFileResponse,
            cancellationToken);

    /// <summary>Writes a text file through the client.</summary>
    public ValueTask<AcpWriteTextFileResponse> WriteTextFileAsync(
        AcpWriteTextFileRequest request,
        CancellationToken cancellationToken = default) =>
        _session.InvokeClientAsync(
            AcpProtocol.Methods.FsWriteTextFile,
            request,
            AcpJsonContext.Default.AcpWriteTextFileRequest,
            AcpJsonContext.Default.AcpWriteTextFileResponse,
            cancellationToken);

    /// <summary>Creates a terminal through the client.</summary>
    public ValueTask<AcpCreateTerminalResponse> CreateTerminalAsync(
        AcpCreateTerminalRequest request,
        CancellationToken cancellationToken = default) =>
        _session.InvokeClientAsync(
            AcpProtocol.Methods.TerminalCreate,
            request,
            AcpJsonContext.Default.AcpCreateTerminalRequest,
            AcpJsonContext.Default.AcpCreateTerminalResponse,
            cancellationToken);

    /// <summary>Reads terminal output through the client.</summary>
    public ValueTask<AcpTerminalOutputResponse> GetTerminalOutputAsync(
        AcpTerminalOutputRequest request,
        CancellationToken cancellationToken = default) =>
        _session.InvokeClientAsync(
            AcpProtocol.Methods.TerminalOutput,
            request,
            AcpJsonContext.Default.AcpTerminalOutputRequest,
            AcpJsonContext.Default.AcpTerminalOutputResponse,
            cancellationToken);

    /// <summary>Waits for a terminal to exit through the client.</summary>
    public ValueTask<AcpWaitForTerminalExitResponse> WaitForTerminalExitAsync(
        AcpWaitForTerminalExitRequest request,
        CancellationToken cancellationToken = default) =>
        _session.InvokeClientAsync(
            AcpProtocol.Methods.TerminalWaitForExit,
            request,
            AcpJsonContext.Default.AcpWaitForTerminalExitRequest,
            AcpJsonContext.Default.AcpWaitForTerminalExitResponse,
            cancellationToken);

    /// <summary>Kills a terminal through the client.</summary>
    public ValueTask<AcpKillTerminalResponse> KillTerminalAsync(
        AcpKillTerminalRequest request,
        CancellationToken cancellationToken = default) =>
        _session.InvokeClientAsync(
            AcpProtocol.Methods.TerminalKill,
            request,
            AcpJsonContext.Default.AcpKillTerminalRequest,
            AcpJsonContext.Default.AcpKillTerminalResponse,
            cancellationToken);

    /// <summary>Releases a terminal through the client.</summary>
    public ValueTask<AcpReleaseTerminalResponse> ReleaseTerminalAsync(
        AcpReleaseTerminalRequest request,
        CancellationToken cancellationToken = default) =>
        _session.InvokeClientAsync(
            AcpProtocol.Methods.TerminalRelease,
            request,
            AcpJsonContext.Default.AcpReleaseTerminalRequest,
            AcpJsonContext.Default.AcpReleaseTerminalResponse,
            cancellationToken);

    /// <summary>Requests structured input through the client.</summary>
    public ValueTask<AcpCreateElicitationResponse> ElicitAsync(
        AcpCreateElicitationRequest request,
        CancellationToken cancellationToken = default) =>
        _session.InvokeClientAsync(
            AcpProtocol.Methods.ElicitationCreate,
            request,
            AcpJsonContext.Default.AcpCreateElicitationRequest,
            AcpJsonContext.Default.AcpCreateElicitationResponse,
            cancellationToken);
}

using Capastrum.Acp.Protocol;

namespace Capastrum.Acp.Server;

/// <summary>
/// Configures an <see cref="AcpAgentServer"/>.
/// </summary>
public sealed class AcpAgentServerOptions
{
    /// <summary>The agent implementation.</summary>
    public required AcpImplementation AgentInfo { get; init; }

    /// <summary>The agent capabilities.</summary>
    public AcpAgentCapabilities AgentCapabilities { get; init; } = new();

    /// <summary>The authentication methods offered by the agent.</summary>
    public IReadOnlyList<AcpAuthMethod> AuthMethods { get; init; } = [];

    /// <summary>Handles authentication.</summary>
    public Func<AcpAuthenticateRequest, CancellationToken, ValueTask<AcpAuthenticateResponse>>? AuthenticateHandler { get; init; }

    /// <summary>Handles logout.</summary>
    public Func<AcpLogoutRequest, CancellationToken, ValueTask<AcpLogoutResponse>>? LogoutHandler { get; init; }

    /// <summary>Creates a session.</summary>
    public Func<AcpNewSessionRequest, CancellationToken, ValueTask<AcpNewSessionResponse>>? NewSessionHandler { get; init; }

    /// <summary>Loads a session.</summary>
    public Func<AcpLoadSessionRequest, CancellationToken, ValueTask<AcpLoadSessionResponse>>? LoadSessionHandler { get; init; }

    /// <summary>Resumes a session.</summary>
    public Func<AcpResumeSessionRequest, CancellationToken, ValueTask<AcpResumeSessionResponse>>? ResumeSessionHandler { get; init; }

    /// <summary>Lists sessions.</summary>
    public Func<AcpListSessionsRequest, CancellationToken, ValueTask<AcpListSessionsResponse>>? ListSessionsHandler { get; init; }

    /// <summary>Closes a session.</summary>
    public Func<AcpCloseSessionRequest, CancellationToken, ValueTask<AcpCloseSessionResponse>>? CloseSessionHandler { get; init; }

    /// <summary>Deletes a session.</summary>
    public Func<AcpDeleteSessionRequest, CancellationToken, ValueTask<AcpDeleteSessionResponse>>? DeleteSessionHandler { get; init; }

    /// <summary>Sets the session mode.</summary>
    public Func<AcpSetSessionModeRequest, CancellationToken, ValueTask<AcpSetSessionModeResponse>>? SetModeHandler { get; init; }

    /// <summary>Sets a session configuration option.</summary>
    public Func<AcpSetSessionConfigOptionRequest, CancellationToken, ValueTask<AcpSetSessionConfigOptionResponse>>? SetConfigOptionHandler { get; init; }

    /// <summary>Processes a prompt turn.</summary>
    public required Func<AcpPromptContext, CancellationToken, ValueTask<AcpPromptResponse>> PromptHandler { get; init; }
}

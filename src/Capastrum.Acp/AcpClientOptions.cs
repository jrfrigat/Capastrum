using Capastrum.Acp.Protocol;

namespace Capastrum.Acp;

/// <summary>
/// Configures an <see cref="AcpAgentClient"/>.
/// </summary>
public sealed class AcpClientOptions
{
    /// <summary>The agent executable. Required by <see cref="AcpAgentClient.StartAsync"/>.</summary>
    public string? Executable { get; init; }

    /// <summary>The executable arguments.</summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];

    /// <summary>The workspace root.</summary>
    public required string WorkspaceRoot { get; init; }

    /// <summary>Additional environment variables.</summary>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }

    /// <summary>The process working directory.</summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>The client implementation.</summary>
    public AcpImplementation ClientInfo { get; init; } = new()
    {
        Name = "Capastrum",
        Version = "1.0.0",
    };

    /// <summary>The client capabilities.</summary>
    public AcpClientCapabilities ClientCapabilities { get; init; } = new()
    {
        FileSystem = new AcpFileSystemCapabilities
        {
            ReadTextFile = true,
            WriteTextFile = true,
        },
        Terminal = true,
    };

    /// <summary>Decides whether a tool action may run.</summary>
    public Func<AcpPermissionRequest, CancellationToken, ValueTask<ConfirmationResult>>? OnActionConfirm { get; init; }

    /// <summary>Handles text file reads.</summary>
    public Func<AcpReadTextFileRequest, CancellationToken, ValueTask<AcpReadTextFileResponse>>? ReadTextFileAsync { get; init; }

    /// <summary>Handles text file writes.</summary>
    public Func<AcpWriteTextFileRequest, CancellationToken, ValueTask<AcpWriteTextFileResponse>>? WriteTextFileAsync { get; init; }

    /// <summary>Handles terminal requests.</summary>
    public AcpTerminalHost? TerminalHost { get; init; }

    /// <summary>Handles elicitation requests.</summary>
    public Func<AcpCreateElicitationRequest, CancellationToken, ValueTask<AcpCreateElicitationResponse>>? ElicitationHandler { get; init; }

    /// <summary>Receives session updates.</summary>
    public Action<AcpSessionNotification>? OnSessionUpdate { get; init; }
}

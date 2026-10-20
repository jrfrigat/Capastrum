using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Capastrum.Acp.Protocol;
using Capastrum.Acp.Protocol.Serialization;
using Capastrum.JsonRpc;
using Capastrum.JsonRpc.Transports;

namespace Capastrum.Acp;

/// <summary>
/// An ACP host that launches an agent process and orchestrates its sessions.
/// </summary>
public sealed class AcpAgentClient : IAsyncDisposable, IDisposable
{
    private readonly JsonRpcPeer _peer;
    private readonly AcpClientOptions _options;
    private readonly AcpTerminalHost _terminalHost;
    private readonly bool _ownsTerminalHost;
    private Func<AcpPermissionRequest, CancellationToken, ValueTask<ConfirmationResult>>? _actionConfirm;
    private Func<AcpReadTextFileRequest, CancellationToken, ValueTask<AcpReadTextFileResponse>>? _readTextFile;
    private Func<AcpWriteTextFileRequest, CancellationToken, ValueTask<AcpWriteTextFileResponse>>? _writeTextFile;
    private Func<AcpCreateElicitationRequest, CancellationToken, ValueTask<AcpCreateElicitationResponse>>? _elicitationHandler;
    private Process? _process;
    private bool _disposed;

    private AcpAgentClient(JsonRpcPeer peer, AcpClientOptions options)
    {
        _peer = peer;
        _options = options;
        _actionConfirm = options.OnActionConfirm;
        _readTextFile = options.ReadTextFileAsync;
        _writeTextFile = options.WriteTextFileAsync;
        _elicitationHandler = options.ElicitationHandler;
        _terminalHost = options.TerminalHost ?? new AcpTerminalHost();
        _ownsTerminalHost = options.TerminalHost is null;

        if (options.OnSessionUpdate is not null)
        {
            SessionUpdated += options.OnSessionUpdate;
        }

        _peer.RequestHandler = HandleRequestAsync;
        _peer.NotificationReceived += OnNotification;
    }

    /// <summary>The agent implementation reported by <c>initialize</c>.</summary>
    public AcpImplementation? AgentInfo { get; private set; }

    /// <summary>The agent capabilities.</summary>
    public AcpAgentCapabilities? AgentCapabilities { get; private set; }

    /// <summary>The authentication methods offered by the agent.</summary>
    public IReadOnlyList<AcpAuthMethod> AuthMethods { get; private set; } = [];

    /// <summary>Raised for every <c>session/update</c> notification.</summary>
    public event Action<AcpSessionNotification>? SessionUpdated;

    /// <summary>Raised when an elicitation completes.</summary>
    public event Action<AcpCompleteElicitationNotification>? ElicitationCompleted;

    /// <summary>Raised for every line the agent writes to stderr.</summary>
    public event Action<string>? StandardErrorReceived;

    /// <summary>
    /// Starts an agent process and initializes the connection.
    /// </summary>
    public static async Task<AcpAgentClient> StartAsync(
        AcpClientOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Executable))
        {
            throw new ArgumentException("An agent executable is required.", nameof(options));
        }

        var startInfo = new ProcessStartInfo(options.Executable)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = options.WorkingDirectory ?? options.WorkspaceRoot,
        };

        foreach (var argument in options.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (options.Environment is not null)
        {
            foreach (var variable in options.Environment)
            {
                startInfo.Environment[variable.Key] = variable.Value;
            }
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{options.Executable}'.");

        var transport = new NewlineJsonTransport(
            process.StandardInput.BaseStream,
            process.StandardOutput.BaseStream);

        try
        {
            var client = await ConnectAsync(transport, options, cancellationToken).ConfigureAwait(false);
            client.AttachProcess(process);
            return client;
        }
        catch
        {
            process.Kill(entireProcessTree: true);
            process.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Connects over an existing transport and initializes the connection.
    /// </summary>
    public static async Task<AcpAgentClient> ConnectAsync(
        IJsonRpcTransport transport,
        AcpClientOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(options);

        var peer = new JsonRpcPeer(transport);
        var client = new AcpAgentClient(peer, options);

        try
        {
            await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
            return client;
        }
        catch
        {
            await client.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Initializes the agent connection.</summary>
    public async ValueTask<AcpInitializeResponse> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var request = new AcpInitializeRequest
        {
            ProtocolVersion = AcpProtocol.Version,
            ClientCapabilities = _options.ClientCapabilities,
            ClientInfo = _options.ClientInfo,
        };

        var response = await SendRequestAsync(
            AcpProtocol.Methods.Initialize,
            request,
            AcpJsonContext.Default.AcpInitializeRequest,
            AcpJsonContext.Default.AcpInitializeResponse,
            cancellationToken).ConfigureAwait(false);

        if (response.ProtocolVersion != AcpProtocol.Version)
        {
            throw new InvalidOperationException(
                $"The agent negotiated ACP version {response.ProtocolVersion}, but version {AcpProtocol.Version} is required.");
        }

        AgentInfo = response.AgentInfo;
        AgentCapabilities = response.AgentCapabilities;
        AuthMethods = response.AuthMethods ?? [];

        return response;
    }

    /// <summary>Authenticates with the agent.</summary>
    public async ValueTask<AcpAuthenticateResponse> AuthenticateAsync(
        string methodId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodId);

        return await SendRequestAsync(
            AcpProtocol.Methods.Authenticate,
            new AcpAuthenticateRequest { MethodId = methodId },
            AcpJsonContext.Default.AcpAuthenticateRequest,
            AcpJsonContext.Default.AcpAuthenticateResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Logs out.</summary>
    public async ValueTask<AcpLogoutResponse> LogoutAsync(CancellationToken cancellationToken = default) =>
        await SendRequestAsync(
            AcpProtocol.Methods.Logout,
            AcpJsonContext.Default.AcpLogoutResponse,
            cancellationToken).ConfigureAwait(false);

    /// <summary>Creates a session.</summary>
    public async ValueTask<AcpNewSessionResponse> NewSessionAsync(
        string cwd,
        IReadOnlyList<AcpMcpServer>? mcpServers = null,
        IReadOnlyList<string>? additionalDirectories = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cwd);

        var request = new AcpNewSessionRequest
        {
            Cwd = cwd,
            McpServers = mcpServers ?? [],
            AdditionalDirectories = additionalDirectories,
        };

        return await SendRequestAsync(
            AcpProtocol.Methods.SessionNew,
            request,
            AcpJsonContext.Default.AcpNewSessionRequest,
            AcpJsonContext.Default.AcpNewSessionResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads a session.</summary>
    public async ValueTask<AcpLoadSessionResponse> LoadSessionAsync(
        string sessionId,
        string cwd,
        IReadOnlyList<AcpMcpServer>? mcpServers = null,
        IReadOnlyList<string>? additionalDirectories = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var request = new AcpLoadSessionRequest
        {
            SessionId = sessionId,
            Cwd = cwd,
            McpServers = mcpServers ?? [],
            AdditionalDirectories = additionalDirectories,
        };

        return await SendRequestAsync(
            AcpProtocol.Methods.SessionLoad,
            request,
            AcpJsonContext.Default.AcpLoadSessionRequest,
            AcpJsonContext.Default.AcpLoadSessionResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resumes a session.</summary>
    public async ValueTask<AcpResumeSessionResponse> ResumeSessionAsync(
        string sessionId,
        string cwd,
        IReadOnlyList<string>? additionalDirectories = null,
        IReadOnlyList<AcpMcpServer>? mcpServers = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var request = new AcpResumeSessionRequest
        {
            SessionId = sessionId,
            Cwd = cwd,
            AdditionalDirectories = additionalDirectories,
            McpServers = mcpServers,
        };

        return await SendRequestAsync(
            AcpProtocol.Methods.SessionResume,
            request,
            AcpJsonContext.Default.AcpResumeSessionRequest,
            AcpJsonContext.Default.AcpResumeSessionResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists sessions.</summary>
    public async ValueTask<AcpListSessionsResponse> ListSessionsAsync(
        string? cwd = null,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var request = new AcpListSessionsRequest { Cwd = cwd, Cursor = cursor };

        return await SendRequestAsync(
            AcpProtocol.Methods.SessionList,
            request,
            AcpJsonContext.Default.AcpListSessionsRequest,
            AcpJsonContext.Default.AcpListSessionsResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Closes a session.</summary>
    public async ValueTask<AcpCloseSessionResponse> CloseSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        return await SendRequestAsync(
            AcpProtocol.Methods.SessionClose,
            new AcpCloseSessionRequest { SessionId = sessionId },
            AcpJsonContext.Default.AcpCloseSessionRequest,
            AcpJsonContext.Default.AcpCloseSessionResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a session.</summary>
    public async ValueTask<AcpDeleteSessionResponse> DeleteSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        return await SendRequestAsync(
            AcpProtocol.Methods.SessionDelete,
            new AcpDeleteSessionRequest { SessionId = sessionId },
            AcpJsonContext.Default.AcpDeleteSessionRequest,
            AcpJsonContext.Default.AcpDeleteSessionResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sets the session mode.</summary>
    public async ValueTask<AcpSetSessionModeResponse> SetModeAsync(
        string sessionId,
        string modeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modeId);

        return await SendRequestAsync(
            AcpProtocol.Methods.SessionSetMode,
            new AcpSetSessionModeRequest { SessionId = sessionId, ModeId = modeId },
            AcpJsonContext.Default.AcpSetSessionModeRequest,
            AcpJsonContext.Default.AcpSetSessionModeResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sets a session configuration option.</summary>
    public async ValueTask<AcpSetSessionConfigOptionResponse> SetConfigOptionAsync(
        string sessionId,
        string configId,
        JsonElement? value = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(configId);

        return await SendRequestAsync(
            AcpProtocol.Methods.SessionSetConfigOption,
            new AcpSetSessionConfigOptionRequest { SessionId = sessionId, ConfigId = configId, Value = value },
            AcpJsonContext.Default.AcpSetSessionConfigOptionRequest,
            AcpJsonContext.Default.AcpSetSessionConfigOptionResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a prompt and waits for the turn to finish.</summary>
    public async ValueTask<AcpPromptResponse> SendPromptAsync(
        string sessionId,
        IReadOnlyList<AcpContentBlock> prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(prompt);

        using var registration = cancellationToken.Register(
            () => { _ = CancelAsync(sessionId, CancellationToken.None).AsTask(); });

        return await SendRequestAsync(
            AcpProtocol.Methods.SessionPrompt,
            new AcpPromptRequest { SessionId = sessionId, Prompt = prompt },
            AcpJsonContext.Default.AcpPromptRequest,
            AcpJsonContext.Default.AcpPromptResponse,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels the current prompt turn.</summary>
    public ValueTask CancelAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var parameters = JsonSerializer.SerializeToElement(
            new AcpCancelNotification { SessionId = sessionId },
            AcpJsonContext.Default.AcpCancelNotification);

        return _peer.NotifyAsync(AcpProtocol.Notifications.SessionCancel, parameters, cancellationToken);
    }

    /// <summary>Registers a session update handler.</summary>
    public AcpAgentClient OnSessionUpdate(Action<AcpSessionNotification> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        SessionUpdated += handler;
        return this;
    }

    /// <summary>Registers the human-in-the-loop permission handler.</summary>
    public AcpAgentClient OnActionConfirm(
        Func<AcpPermissionRequest, CancellationToken, ValueTask<ConfirmationResult>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _actionConfirm = handler;
        return this;
    }

    /// <summary>Registers a text file read handler.</summary>
    public AcpAgentClient OnReadTextFile(
        Func<AcpReadTextFileRequest, CancellationToken, ValueTask<AcpReadTextFileResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _readTextFile = handler;
        return this;
    }

    /// <summary>Registers a text file write handler.</summary>
    public AcpAgentClient OnWriteTextFile(
        Func<AcpWriteTextFileRequest, CancellationToken, ValueTask<AcpWriteTextFileResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _writeTextFile = handler;
        return this;
    }

    /// <summary>Registers an elicitation handler.</summary>
    public AcpAgentClient OnElicitation(
        Func<AcpCreateElicitationRequest, CancellationToken, ValueTask<AcpCreateElicitationResponse>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _elicitationHandler = handler;
        return this;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _peer.NotificationReceived -= OnNotification;
        await _peer.DisposeAsync().ConfigureAwait(false);

        if (_ownsTerminalHost)
        {
            await _terminalHost.DisposeAsync().ConfigureAwait(false);
        }

        var process = _process;
        _process = null;

        if (process is not null)
        {
            TryKill(process);
            process.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private async ValueTask<JsonRpcResponse> HandleRequestAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        return request.Method switch
        {
            AcpProtocol.Methods.SessionRequestPermission => await HandlePermissionAsync(request, cancellationToken)
                .ConfigureAwait(false),
            AcpProtocol.Methods.FsReadTextFile => await HandleReadTextFileAsync(request, cancellationToken)
                .ConfigureAwait(false),
            AcpProtocol.Methods.FsWriteTextFile => await HandleWriteTextFileAsync(request, cancellationToken)
                .ConfigureAwait(false),
            AcpProtocol.Methods.TerminalCreate => Success(
                request.Id,
                await _terminalHost.CreateAsync(
                    Deserialize(request, AcpJsonContext.Default.AcpCreateTerminalRequest),
                    cancellationToken).ConfigureAwait(false),
                AcpJsonContext.Default.AcpCreateTerminalResponse),
            AcpProtocol.Methods.TerminalOutput => Success(
                request.Id,
                await _terminalHost.GetOutputAsync(
                    Deserialize(request, AcpJsonContext.Default.AcpTerminalOutputRequest),
                    cancellationToken).ConfigureAwait(false),
                AcpJsonContext.Default.AcpTerminalOutputResponse),
            AcpProtocol.Methods.TerminalWaitForExit => Success(
                request.Id,
                await _terminalHost.WaitForExitAsync(
                    Deserialize(request, AcpJsonContext.Default.AcpWaitForTerminalExitRequest),
                    cancellationToken).ConfigureAwait(false),
                AcpJsonContext.Default.AcpWaitForTerminalExitResponse),
            AcpProtocol.Methods.TerminalKill => Success(
                request.Id,
                await _terminalHost.KillAsync(
                    Deserialize(request, AcpJsonContext.Default.AcpKillTerminalRequest),
                    cancellationToken).ConfigureAwait(false),
                AcpJsonContext.Default.AcpKillTerminalResponse),
            AcpProtocol.Methods.TerminalRelease => Success(
                request.Id,
                await _terminalHost.ReleaseAsync(
                    Deserialize(request, AcpJsonContext.Default.AcpReleaseTerminalRequest),
                    cancellationToken).ConfigureAwait(false),
                AcpJsonContext.Default.AcpReleaseTerminalResponse),
            AcpProtocol.Methods.ElicitationCreate => await HandleElicitationAsync(request, cancellationToken)
                .ConfigureAwait(false),
            _ => throw new JsonRpcException(
                JsonRpcErrorCode.MethodNotFound,
                $"Method '{request.Method}' is not handled by the ACP host."),
        };
    }

    private async ValueTask<JsonRpcResponse> HandlePermissionAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var parameters = Deserialize(request, AcpJsonContext.Default.AcpRequestPermissionRequest);
        var permission = new AcpPermissionRequest(parameters);
        var result = _actionConfirm is null
            ? ConfirmationResult.Denied
            : await _actionConfirm(permission, cancellationToken).ConfigureAwait(false);

        return Success(
            request.Id,
            new AcpRequestPermissionResponse { Outcome = result.ToOutcome(parameters.Options) },
            AcpJsonContext.Default.AcpRequestPermissionResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleReadTextFileAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _readTextFile
            ?? throw new JsonRpcException(
                JsonRpcErrorCode.MethodNotFound,
                "Text file reads are not configured on this host.");

        var parameters = Deserialize(request, AcpJsonContext.Default.AcpReadTextFileRequest);
        var response = await handler(parameters, cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpReadTextFileResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleWriteTextFileAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _writeTextFile
            ?? throw new JsonRpcException(
                JsonRpcErrorCode.MethodNotFound,
                "Text file writes are not configured on this host.");

        var parameters = Deserialize(request, AcpJsonContext.Default.AcpWriteTextFileRequest);
        var response = await handler(parameters, cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpWriteTextFileResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleElicitationAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var parameters = Deserialize(request, AcpJsonContext.Default.AcpCreateElicitationRequest);
        var response = _elicitationHandler is null
            ? new AcpCreateElicitationResponse()
            : await _elicitationHandler(parameters, cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpCreateElicitationResponse);
    }

    private void OnNotification(JsonRpcNotification notification)
    {
        if (notification.Method == AcpProtocol.Notifications.SessionUpdate
            && notification.Params is { } parameters)
        {
            var update = parameters.Deserialize(AcpJsonContext.Default.AcpSessionNotification);
            if (update is not null)
            {
                SessionUpdated?.Invoke(update);
            }
        }
        else if (notification.Method == AcpProtocol.Notifications.ElicitationComplete
                 && notification.Params is { } elicitationParameters)
        {
            var completed = elicitationParameters.Deserialize(
                AcpJsonContext.Default.AcpCompleteElicitationNotification);
            if (completed is not null)
            {
                ElicitationCompleted?.Invoke(completed);
            }
        }
    }

    private async ValueTask<TResult> SendRequestAsync<TParameters, TResult>(
        string method,
        TParameters parameters,
        JsonTypeInfo<TParameters> parametersTypeInfo,
        JsonTypeInfo<TResult> resultTypeInfo,
        CancellationToken cancellationToken)
    {
        var parametersElement = JsonSerializer.SerializeToElement(parameters, parametersTypeInfo);
        var result = await _peer.InvokeAsync(method, parametersElement, cancellationToken).ConfigureAwait(false);

        return result.Deserialize(resultTypeInfo)
            ?? throw new JsonRpcException(
                JsonRpcErrorCode.InternalError,
                $"The agent returned an empty result for '{method}'.");
    }

    private async ValueTask<TResult> SendRequestAsync<TResult>(
        string method,
        JsonTypeInfo<TResult> resultTypeInfo,
        CancellationToken cancellationToken)
    {
        var result = await _peer.InvokeAsync(method, parameters: null, cancellationToken).ConfigureAwait(false);

        return result.Deserialize(resultTypeInfo)
            ?? throw new JsonRpcException(
                JsonRpcErrorCode.InternalError,
                $"The agent returned an empty result for '{method}'.");
    }

    private static T Deserialize<T>(JsonRpcRequest request, JsonTypeInfo<T> typeInfo)
    {
        if (request.Params is not { } parameters)
        {
            throw new JsonRpcException(
                JsonRpcErrorCode.InvalidParams,
                $"Method '{request.Method}' requires parameters.");
        }

        return parameters.Deserialize(typeInfo)
            ?? throw new JsonRpcException(
                JsonRpcErrorCode.InvalidParams,
                $"Method '{request.Method}' received empty parameters.");
    }

    private static JsonRpcResponse Success<T>(
        JsonRpcId id,
        T result,
        JsonTypeInfo<T> typeInfo) => new()
    {
        Id = id,
        Result = JsonSerializer.SerializeToElement(result, typeInfo),
    };

    private void AttachProcess(Process process)
    {
        _process = process;

        _ = Task.Run(async () =>
        {
            try
            {
                while (await process.StandardError.ReadLineAsync().ConfigureAwait(false) is { } line)
                {
                    StandardErrorReceived?.Invoke(line);
                }
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        });
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}

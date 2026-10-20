using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Capastrum.Acp.Protocol;
using Capastrum.Acp.Protocol.Serialization;
using Capastrum.JsonRpc;

namespace Capastrum.Acp.Server;

/// <summary>
/// Handles one ACP connection on the agent side.
/// </summary>
public sealed class AcpAgentSession : IAsyncDisposable
{
    private readonly AcpAgentServerOptions _options;
    private readonly JsonRpcPeer _peer;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _promptCancellations = new(StringComparer.Ordinal);
    private bool _disposed;

    internal AcpAgentSession(AcpAgentServerOptions options, JsonRpcPeer peer)
    {
        _options = options;
        _peer = peer;
    }

    /// <summary>Handles a client request.</summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "An unexpected handler failure must be returned as a JSON-RPC internal error.")]
    public async ValueTask<JsonRpcResponse> HandleRequestAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return await DispatchAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonRpcException exception)
        {
            return Error(request.Id, exception.Code, exception.Message, exception.Data);
        }
        catch (Exception exception)
        {
            return Error(request.Id, JsonRpcErrorCode.InternalError, exception.Message, data: null);
        }
    }

    /// <summary>Handles client notifications.</summary>
    public void HandleNotification(JsonRpcNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification.Method != AcpProtocol.Notifications.SessionCancel || notification.Params is not { } parameters)
        {
            return;
        }

        var cancel = parameters.Deserialize(AcpJsonContext.Default.AcpCancelNotification);
        if (cancel is not null && _promptCancellations.TryGetValue(cancel.SessionId, out var cancellation))
        {
            cancellation.Cancel();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        foreach (var cancellation in _promptCancellations)
        {
            cancellation.Value.Cancel();
            cancellation.Value.Dispose();
        }

        _promptCancellations.Clear();
        return ValueTask.CompletedTask;
    }

    internal ValueTask SendUpdateAsync(
        string sessionId,
        AcpSessionUpdate update,
        CancellationToken cancellationToken)
    {
        var notification = new AcpSessionNotification
        {
            SessionId = sessionId,
            Update = update,
        };

        var parameters = JsonSerializer.SerializeToElement(
            notification,
            AcpJsonContext.Default.AcpSessionNotification);

        return _peer.NotifyAsync(AcpProtocol.Notifications.SessionUpdate, parameters, cancellationToken);
    }

    internal async ValueTask<TResult> InvokeClientAsync<TParameters, TResult>(
        string method,
        TParameters parameters,
        JsonTypeInfo<TParameters> parametersTypeInfo,
        JsonTypeInfo<TResult> resultTypeInfo,
        CancellationToken cancellationToken)
    {
        var element = JsonSerializer.SerializeToElement(parameters, parametersTypeInfo);
        var result = await _peer.InvokeAsync(method, element, cancellationToken).ConfigureAwait(false);

        return result.Deserialize(resultTypeInfo)
            ?? throw new JsonRpcException(
                JsonRpcErrorCode.InternalError,
                $"The client returned an empty result for '{method}'.");
    }

    internal ValueTask<AcpRequestPermissionResponse> RequestPermissionAsync(
        AcpRequestPermissionRequest request,
        CancellationToken cancellationToken) =>
        InvokeClientAsync(
            AcpProtocol.Methods.SessionRequestPermission,
            request,
            AcpJsonContext.Default.AcpRequestPermissionRequest,
            AcpJsonContext.Default.AcpRequestPermissionResponse,
            cancellationToken);

    private async ValueTask<JsonRpcResponse> DispatchAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        return request.Method switch
        {
            AcpProtocol.Methods.Initialize => Success(
                request.Id,
                new AcpInitializeResponse
                {
                    ProtocolVersion = AcpProtocol.Version,
                    AgentInfo = _options.AgentInfo,
                    AgentCapabilities = _options.AgentCapabilities,
                    AuthMethods = _options.AuthMethods,
                },
                AcpJsonContext.Default.AcpInitializeResponse),

            AcpProtocol.Methods.Authenticate => await HandleAuthenticateAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.Logout => await HandleLogoutAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.SessionNew => await HandleNewSessionAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.SessionLoad => await HandleLoadSessionAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.SessionResume => await HandleResumeSessionAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.SessionList => await HandleListSessionsAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.SessionClose => await HandleCloseSessionAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.SessionDelete => await HandleDeleteSessionAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.SessionSetMode => await HandleSetModeAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.SessionSetConfigOption => await HandleSetConfigOptionAsync(request, cancellationToken)
                .ConfigureAwait(false),

            AcpProtocol.Methods.SessionPrompt => await HandlePromptAsync(request, cancellationToken)
                .ConfigureAwait(false),

            _ => throw new JsonRpcException(
                JsonRpcErrorCode.MethodNotFound,
                $"Method '{request.Method}' is not supported by the agent."),
        };
    }

    private async ValueTask<JsonRpcResponse> HandleAuthenticateAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _options.AuthenticateHandler
            ?? throw MissingHandler(request.Method);

        var response = await handler(
            Deserialize(request, AcpJsonContext.Default.AcpAuthenticateRequest),
            cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpAuthenticateResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleLogoutAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var response = _options.LogoutHandler is null
            ? new AcpLogoutResponse()
            : await _options.LogoutHandler(
                Deserialize(request, AcpJsonContext.Default.AcpLogoutRequest),
                cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpLogoutResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleNewSessionAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _options.NewSessionHandler
            ?? throw MissingHandler(request.Method);

        var response = await handler(
            Deserialize(request, AcpJsonContext.Default.AcpNewSessionRequest),
            cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpNewSessionResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleLoadSessionAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _options.LoadSessionHandler
            ?? throw MissingHandler(request.Method);

        var response = await handler(
            Deserialize(request, AcpJsonContext.Default.AcpLoadSessionRequest),
            cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpLoadSessionResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleResumeSessionAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _options.ResumeSessionHandler
            ?? throw MissingHandler(request.Method);

        var response = await handler(
            Deserialize(request, AcpJsonContext.Default.AcpResumeSessionRequest),
            cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpResumeSessionResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleListSessionsAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _options.ListSessionsHandler
            ?? throw MissingHandler(request.Method);

        var response = await handler(
            Deserialize(request, AcpJsonContext.Default.AcpListSessionsRequest),
            cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpListSessionsResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleCloseSessionAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _options.CloseSessionHandler
            ?? throw MissingHandler(request.Method);

        var response = await handler(
            Deserialize(request, AcpJsonContext.Default.AcpCloseSessionRequest),
            cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpCloseSessionResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleDeleteSessionAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _options.DeleteSessionHandler
            ?? throw MissingHandler(request.Method);

        var response = await handler(
            Deserialize(request, AcpJsonContext.Default.AcpDeleteSessionRequest),
            cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpDeleteSessionResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleSetModeAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _options.SetModeHandler
            ?? throw MissingHandler(request.Method);

        var response = await handler(
            Deserialize(request, AcpJsonContext.Default.AcpSetSessionModeRequest),
            cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpSetSessionModeResponse);
    }

    private async ValueTask<JsonRpcResponse> HandleSetConfigOptionAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var handler = _options.SetConfigOptionHandler
            ?? throw MissingHandler(request.Method);

        var response = await handler(
            Deserialize(request, AcpJsonContext.Default.AcpSetSessionConfigOptionRequest),
            cancellationToken).ConfigureAwait(false);

        return Success(request.Id, response, AcpJsonContext.Default.AcpSetSessionConfigOptionResponse);
    }

    private async ValueTask<JsonRpcResponse> HandlePromptAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var prompt = Deserialize(request, AcpJsonContext.Default.AcpPromptRequest);
        using var promptCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _promptCancellations[prompt.SessionId] = promptCancellation;

        try
        {
            var context = new AcpPromptContext(this, request.Id, prompt, promptCancellation.Token);
            var response = await _options.PromptHandler(context, promptCancellation.Token).ConfigureAwait(false);

            return Success(request.Id, response, AcpJsonContext.Default.AcpPromptResponse);
        }
        catch (OperationCanceledException) when (promptCancellation.IsCancellationRequested)
        {
            return Success(
                request.Id,
                new AcpPromptResponse { StopReason = AcpStopReason.Cancelled },
                AcpJsonContext.Default.AcpPromptResponse);
        }
        finally
        {
            _promptCancellations.TryRemove(prompt.SessionId, out _);
        }
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

    private static JsonRpcResponse Success<T>(JsonRpcId id, T result, JsonTypeInfo<T> typeInfo) => new()
    {
        Id = id,
        Result = JsonSerializer.SerializeToElement(result, typeInfo),
    };

    private static JsonRpcResponse Error(JsonRpcId id, int code, string message, JsonElement? data) => new()
    {
        Id = id,
        Error = new JsonRpcError { Code = code, Message = message, Data = data },
    };

    private static JsonRpcException MissingHandler(string method) => new(
        JsonRpcErrorCode.MethodNotFound,
        $"Method '{method}' is not configured on this agent.");
}

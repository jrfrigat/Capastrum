using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Capastrum.JsonRpc;

/// <summary>
/// Correlates JSON-RPC requests and responses over an <see cref="IJsonRpcTransport"/>.
/// </summary>
public sealed class JsonRpcPeer : IAsyncDisposable
{
    private readonly IJsonRpcTransport _transport;
    private readonly ConcurrentDictionary<JsonRpcId, TaskCompletionSource<JsonRpcResponse>> _pending = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Task _readLoop;
    private Exception? _readLoopFailure;
    private long _nextId;
    private bool _disposed;

    /// <summary>
    /// Creates a peer and starts reading from the transport.
    /// </summary>
    public JsonRpcPeer(IJsonRpcTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);

        _transport = transport;
        _readLoop = Task.Run(ReadLoopAsync);
    }

    /// <summary>
    /// Handles requests received from the remote peer. A null handler answers method-not-found.
    /// </summary>
    public Func<JsonRpcRequest, CancellationToken, ValueTask<JsonRpcResponse>>? RequestHandler { get; set; }

    /// <summary>
    /// Raised for every notification received from the remote peer.
    /// </summary>
    public event Action<JsonRpcNotification>? NotificationReceived;

    /// <summary>
    /// Sends a request and returns its raw result.
    /// </summary>
    public async ValueTask<JsonElement> InvokeAsync(
        string method,
        JsonElement? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        if (_readLoopFailure is { } failure)
        {
            throw new InvalidOperationException("The JSON-RPC read loop has stopped.", failure);
        }

        var id = new JsonRpcId(Interlocked.Increment(ref _nextId));
        var completion = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pending.TryAdd(id, completion))
        {
            throw new InvalidOperationException($"A request with id '{id}' is already pending.");
        }

        try
        {
            await _transport.SendAsync(
                new JsonRpcRequest { Id = id, Method = method, Params = parameters },
                cancellationToken).ConfigureAwait(false);

            JsonRpcResponse response;
            try
            {
                response = await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _pending.TryRemove(id, out _);
                throw;
            }

            if (response.Error is { } error)
            {
                throw new JsonRpcException(error.Code, error.Message, error.Data);
            }

            return response.Result ?? default;
        }
        catch
        {
            _pending.TryRemove(id, out _);
            throw;
        }
    }

    /// <summary>
    /// Sends a request and deserializes its result.
    /// </summary>
    public async ValueTask<TResult?> InvokeAsync<TResult>(
        string method,
        JsonElement? parameters = null,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await InvokeAsync(method, parameters, cancellationToken).ConfigureAwait(false);
        if (result.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return default;
        }

        return result.Deserialize<TResult>(options ?? JsonSerializerOptions.Default);
    }

    /// <summary>
    /// Sends a request and deserializes its result using source-generated metadata.
    /// </summary>
    public async ValueTask<TResult?> InvokeAsync<TResult>(
        string method,
        JsonElement? parameters,
        JsonTypeInfo<TResult> resultTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultTypeInfo);

        var result = await InvokeAsync(method, parameters, cancellationToken).ConfigureAwait(false);
        if (result.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return default;
        }

        return result.Deserialize(resultTypeInfo);
    }

    /// <summary>
    /// Serializes typed parameters and sends a request.
    /// </summary>
    public ValueTask<JsonElement> InvokeAsync<TParameters>(
        string method,
        TParameters parameters,
        JsonTypeInfo<TParameters> parametersTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parametersTypeInfo);

        var element = JsonSerializer.SerializeToElement(parameters, parametersTypeInfo);
        return InvokeAsync(method, element, cancellationToken);
    }

    /// <summary>
    /// Sends a notification.
    /// </summary>
    public ValueTask NotifyAsync(
        string method,
        JsonElement? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return _transport.SendAsync(
            new JsonRpcNotification { Method = method, Params = parameters },
            cancellationToken);
    }

    /// <summary>
    /// Sends a successful response.
    /// </summary>
    public ValueTask SendResponseAsync(
        JsonRpcId? id,
        JsonElement? result = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _transport.SendAsync(
            new JsonRpcResponse { Id = id, Result = result },
            cancellationToken);
    }

    /// <summary>
    /// Sends an error response.
    /// </summary>
    public ValueTask SendErrorAsync(
        JsonRpcId? id,
        int code,
        string message,
        JsonElement? data = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _transport.SendAsync(
            new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError { Code = code, Message = message, Data = data },
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _disposeCts.CancelAsync().ConfigureAwait(false);

        foreach (var pending in _pending)
        {
            pending.Value.TrySetException(new ObjectDisposedException(nameof(JsonRpcPeer)));
        }

        _pending.Clear();

        try
        {
            await _readLoop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        await _transport.DisposeAsync().ConfigureAwait(false);
        _disposeCts.Dispose();
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            await foreach (var message in _transport.ReadAsync(_disposeCts.Token).ConfigureAwait(false))
            {
                switch (message)
                {
                    case JsonRpcResponse response when response.Id is { } id:
                        if (_pending.TryRemove(id, out var completion))
                        {
                            completion.TrySetResult(response);
                        }

                        break;

                    case JsonRpcRequest request:
                        _ = HandleRequestAsync(request);
                        break;

                    case JsonRpcNotification notification:
                        NotificationReceived?.Invoke(notification);
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _readLoopFailure = exception;
            FailPending(exception);
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A handler failure must be returned to the caller as a JSON-RPC error.")]
    private async Task HandleRequestAsync(JsonRpcRequest request)
    {
        try
        {
            var handler = RequestHandler;
            if (handler is null)
            {
                await SendErrorAsync(
                    request.Id,
                    JsonRpcErrorCode.MethodNotFound,
                    $"Method '{request.Method}' is not handled.",
                    cancellationToken: _disposeCts.Token).ConfigureAwait(false);
                return;
            }

            var response = await handler(request, _disposeCts.Token).ConfigureAwait(false);
            if (response.Id is null)
            {
                response = new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = response.Result,
                    Error = response.Error,
                };
            }

            await _transport.SendAsync(response, _disposeCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
        }
        catch (JsonRpcException exception)
        {
            await TrySendErrorAsync(request.Id, exception.Code, exception.Message, exception.Data).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await TrySendErrorAsync(
                request.Id,
                JsonRpcErrorCode.InternalError,
                exception.Message,
                data: null).ConfigureAwait(false);
        }
    }

    private async Task TrySendErrorAsync(JsonRpcId id, int code, string message, JsonElement? data)
    {
        try
        {
            await SendErrorAsync(id, code, message, data, _disposeCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            _readLoopFailure ??= new InvalidOperationException("Failed to send a JSON-RPC error response.");
        }
    }

    private void FailPending(Exception exception)
    {
        foreach (var pending in _pending)
        {
            pending.Value.TrySetException(exception);
        }

        _pending.Clear();
    }
}

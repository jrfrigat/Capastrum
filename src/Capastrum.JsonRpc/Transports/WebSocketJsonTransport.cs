using System.Net.WebSockets;
using System.Text.Json;
using Capastrum.JsonRpc.Serialization;

namespace Capastrum.JsonRpc.Transports;

/// <summary>
/// Carries JSON-RPC messages as text frames over a WebSocket.
/// </summary>
public sealed class WebSocketJsonTransport : IJsonRpcTransport
{
    private readonly WebSocket _socket;
    private readonly bool _ownsSocket;
    private readonly int _maxMessageBytes;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Creates a transport over an existing WebSocket.
    /// </summary>
    public WebSocketJsonTransport(
        WebSocket socket,
        bool ownsSocket = false,
        int maxMessageBytes = 4 * 1024 * 1024)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessageBytes);

        _socket = socket;
        _ownsSocket = ownsSocket;
        _maxMessageBytes = maxMessageBytes;
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonRpcJsonContext.Default.JsonRpcMessage);

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _socket.SendAsync(
                payload,
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<JsonRpcMessage> ReadAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var buffer = new byte[16 * 1024];
        using var messageBuffer = new MemoryStream();

        while (_socket.State is WebSocketState.Open or WebSocketState.CloseSent)
        {
            ValueWebSocketReceiveResult receiveResult;

            do
            {
                receiveResult = await _socket.ReceiveAsync(
                    buffer.AsMemory(),
                    cancellationToken).ConfigureAwait(false);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await _socket.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure,
                        statusDescription: null,
                        cancellationToken).ConfigureAwait(false);
                    yield break;
                }

                if (receiveResult.MessageType != WebSocketMessageType.Text)
                {
                    throw new JsonException("A JSON-RPC WebSocket transport accepts text frames only.");
                }

                messageBuffer.Write(buffer, 0, receiveResult.Count);

                if (messageBuffer.Length > _maxMessageBytes)
                {
                    throw new JsonException(
                        $"The JSON-RPC message exceeds the {_maxMessageBytes} byte limit.");
                }
            }
            while (!receiveResult.EndOfMessage);

            messageBuffer.Position = 0;
            var message = await JsonSerializer.DeserializeAsync(
                messageBuffer,
                JsonRpcJsonContext.Default.JsonRpcMessage,
                cancellationToken).ConfigureAwait(false);

            messageBuffer.SetLength(0);

            if (message is not null)
            {
                yield return message;
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_ownsSocket)
        {
            if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await _socket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    statusDescription: null,
                    CancellationToken.None).ConfigureAwait(false);
            }

            _socket.Dispose();
        }

        _sendLock.Dispose();
    }
}

namespace Capastrum.JsonRpc;

/// <summary>
/// A bidirectional channel that carries JSON-RPC messages.
/// </summary>
public interface IJsonRpcTransport : IAsyncDisposable
{
    /// <summary>
    /// Sends a message to the remote peer.
    /// </summary>
    ValueTask SendAsync(JsonRpcMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads messages from the remote peer until the transport closes.
    /// </summary>
    IAsyncEnumerable<JsonRpcMessage> ReadAsync(CancellationToken cancellationToken = default);
}

using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Channels;
using Capastrum.JsonRpc.Serialization;

namespace Capastrum.JsonRpc.Transports;

/// <summary>
/// Sends JSON-RPC messages as HTTP POST requests and reads the JSON responses.
/// </summary>
public sealed class HttpJsonTransport : IJsonRpcTransport
{
    private readonly HttpClient _client;
    private readonly Uri _endpoint;
    private readonly bool _ownsClient;
    private readonly Channel<JsonRpcMessage> _incoming = Channel.CreateUnbounded<JsonRpcMessage>();
    private bool _disposed;

    /// <summary>
    /// Creates a transport that posts messages to the given endpoint.
    /// </summary>
    public HttpJsonTransport(HttpClient client, Uri endpoint, bool ownsClient = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(endpoint);

        if (!endpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("The endpoint must be absolute.", nameof(endpoint));
        }

        _client = client;
        _endpoint = endpoint;
        _ownsClient = ownsClient;
    }

    /// <summary>
    /// Allows the owner to add protocol headers to every outgoing request.
    /// </summary>
    public Action<HttpRequestMessage, JsonRpcMessage>? ConfigureRequest { get; set; }

    /// <inheritdoc />
    public async ValueTask SendAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonRpcJsonContext.Default.JsonRpcMessage);

        using var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = content,
        };

        ConfigureRequest?.Invoke(request, message);

        using var response = await _client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The JSON-RPC endpoint returned {(int)response.StatusCode} ({response.ReasonPhrase}).",
                inner: null,
                response.StatusCode);
        }

        if (message is JsonRpcNotification)
        {
            return;
        }

        var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (responseBytes.Length == 0)
        {
            return;
        }

        var reply = JsonSerializer.Deserialize(responseBytes, JsonRpcJsonContext.Default.JsonRpcMessage);
        if (reply is not null)
        {
            await _incoming.Writer.WriteAsync(reply, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<JsonRpcMessage> ReadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _incoming.Reader.ReadAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        _incoming.Writer.TryComplete();

        if (_ownsClient)
        {
            _client.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}

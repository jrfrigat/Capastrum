using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using Capastrum.JsonRpc.Serialization;

namespace Capastrum.JsonRpc.Transports;

/// <summary>
/// Carries JSON-RPC messages as newline-delimited JSON over a pair of streams.
/// </summary>
public sealed class NewlineJsonTransport : IJsonRpcTransport
{
    private static readonly ReadOnlyMemory<byte> NewLine = new byte[] { (byte)'\n' };

    private readonly Stream _input;
    private readonly Stream _output;
    private readonly bool _ownsStreams;
    private readonly int _maxMessageBytes;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly PipeReader _reader;
    private bool _readStarted;
    private bool _disposed;

    /// <summary>
    /// Creates a transport over separate input and output streams.
    /// </summary>
    public NewlineJsonTransport(
        Stream input,
        Stream output,
        bool ownsStreams = false,
        int maxMessageBytes = 4 * 1024 * 1024)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessageBytes);

        _input = input;
        _output = output;
        _ownsStreams = ownsStreams;
        _maxMessageBytes = maxMessageBytes;
        _reader = PipeReader.Create(input);
    }

    /// <summary>
    /// Creates a transport over one duplex stream.
    /// </summary>
    public NewlineJsonTransport(
        Stream duplex,
        bool ownsStream = false,
        int maxMessageBytes = 4 * 1024 * 1024)
        : this(duplex, duplex, ownsStream, maxMessageBytes)
    {
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonRpcJsonContext.Default.JsonRpcMessage);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _output.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await _output.WriteAsync(NewLine, cancellationToken).ConfigureAwait(false);
            await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<JsonRpcMessage> ReadAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_readStarted)
        {
            throw new InvalidOperationException("The transport can be read only once.");
        }

        _readStarted = true;

        try
        {
            while (true)
            {
                var readResult = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = readResult.Buffer;

                try
                {
                    while (TryReadLine(ref buffer, out var line))
                    {
                        var message = DeserializeMessage(line, _maxMessageBytes);
                        if (message is not null)
                        {
                            yield return message;
                        }
                    }
                }
                finally
                {
                    _reader.AdvanceTo(buffer.Start, buffer.End);
                }

                if (readResult.IsCompleted)
                {
                    if (!buffer.IsEmpty)
                    {
                        var finalLine = buffer;
                        if (finalLine.Length > 0
                            && finalLine.Slice(finalLine.Length - 1, 1).FirstSpan[0] == (byte)'\r')
                        {
                            finalLine = finalLine.Slice(0, finalLine.Length - 1);
                        }

                        var message = DeserializeMessage(finalLine, _maxMessageBytes);
                        if (message is not null)
                        {
                            yield return message;
                        }
                    }

                    break;
                }
            }
        }
        finally
        {
            await _reader.CompleteAsync().ConfigureAwait(false);
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

        await _reader.CompleteAsync().ConfigureAwait(false);

        if (_ownsStreams)
        {
            await _input.DisposeAsync().ConfigureAwait(false);
            if (!ReferenceEquals(_input, _output))
            {
                await _output.DisposeAsync().ConfigureAwait(false);
            }
        }

        _writeLock.Dispose();
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var position = buffer.PositionOf((byte)'\n');
        if (position is null)
        {
            line = default;
            return false;
        }

        line = buffer.Slice(0, position.Value);

        if (line.Length > 0 && line.Slice(line.Length - 1, 1).FirstSpan[0] == (byte)'\r')
        {
            line = line.Slice(0, line.Length - 1);
        }

        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
        return true;
    }

    private static JsonRpcMessage? DeserializeMessage(ReadOnlySequence<byte> line, int maxMessageBytes)
    {
        if (line.IsEmpty)
        {
            return null;
        }

        if (line.Length > maxMessageBytes)
        {
            throw new JsonException($"The JSON-RPC message exceeds the {maxMessageBytes} byte limit.");
        }

        return line.IsSingleSegment
            ? JsonSerializer.Deserialize(line.FirstSpan, JsonRpcJsonContext.Default.JsonRpcMessage)
            : JsonSerializer.Deserialize(line.ToArray(), JsonRpcJsonContext.Default.JsonRpcMessage);
    }
}

using System.Text;
using System.Text.Json;
using Capastrum.JsonRpc.Transports;
using Xunit;

namespace Capastrum.JsonRpc.Tests;

public sealed class NewlineJsonTransportTests
{
    [Fact]
    public async Task SendsNewlineDelimitedJson()
    {
        using var output = new MemoryStream();
        await using var transport = new NewlineJsonTransport(Stream.Null, output);

        await transport.SendAsync(new JsonRpcNotification { Method = "ping" });

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.EndsWith("\n", text, StringComparison.Ordinal);

        var message = JsonSerializer.Deserialize<JsonRpcMessage>(text.TrimEnd('\n'));
        var notification = Assert.IsType<JsonRpcNotification>(message);
        Assert.Equal("ping", notification.Method);
    }

    [Fact]
    public async Task ReadsMultipleMessagesAndCrLf()
    {
        const string json = """
            {"jsonrpc":"2.0","method":"one"}
            {"jsonrpc":"2.0","method":"two"}
            """;
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(json.Replace("\n", "\r\n", StringComparison.Ordinal)));
        await using var transport = new NewlineJsonTransport(input, Stream.Null);

        var messages = new List<JsonRpcMessage>();
        await foreach (var message in transport.ReadAsync())
        {
            messages.Add(message);
        }

        Assert.Equal(2, messages.Count);
        Assert.Equal("one", Assert.IsType<JsonRpcNotification>(messages[0]).Method);
        Assert.Equal("two", Assert.IsType<JsonRpcNotification>(messages[1]).Method);
    }

    [Fact]
    public async Task ReadsAcrossChunkBoundaries()
    {
        const string json = """
            {"jsonrpc":"2.0","method":"first"}
            {"jsonrpc":"2.0","method":"second"}
            """;
        using var input = new ChunkedReadStream(Encoding.UTF8.GetBytes(json), chunkSize: 3);
        await using var transport = new NewlineJsonTransport(input, Stream.Null);

        var methods = new List<string>();
        await foreach (var message in transport.ReadAsync())
        {
            methods.Add(Assert.IsType<JsonRpcNotification>(message).Method);
        }

        Assert.Equal(["first", "second"], methods);
    }

    [Fact]
    public async Task RejectsOversizedMessage()
    {
        const string json = """{"jsonrpc":"2.0","method":"a very long method name"}""" + "\n";
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await using var transport = new NewlineJsonTransport(input, Stream.Null, maxMessageBytes: 16);

        await Assert.ThrowsAsync<JsonException>(async () =>
        {
            await foreach (var _ in transport.ReadAsync())
            {
            }
        });
    }

    private sealed class ChunkedReadStream(byte[] data, int chunkSize) : Stream
    {
        private int _position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var available = Math.Min(Math.Min(buffer.Length, chunkSize), data.Length - _position);
            if (available <= 0)
            {
                return 0;
            }

            data.AsSpan(_position, available).CopyTo(buffer);
            _position += available;
            return available;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var available = Math.Min(Math.Min(buffer.Length, chunkSize), data.Length - _position);
            if (available <= 0)
            {
                return ValueTask.FromResult(0);
            }

            data.AsMemory(_position, available).CopyTo(buffer);
            _position += available;
            return ValueTask.FromResult(available);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

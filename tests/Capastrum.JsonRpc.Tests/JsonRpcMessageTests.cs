using System.Text.Json;
using Xunit;

namespace Capastrum.JsonRpc.Tests;

public sealed class JsonRpcMessageTests
{
    [Fact]
    public void RoundTripsRequest()
    {
        var request = new JsonRpcRequest
        {
            Id = new JsonRpcId("request-1"),
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(new { name = "echo" }),
        };

        var json = JsonSerializer.Serialize<JsonRpcMessage>(request);
        var parsed = Assert.IsType<JsonRpcRequest>(JsonSerializer.Deserialize<JsonRpcMessage>(json));

        Assert.Equal(request.Id, parsed.Id);
        Assert.Equal(request.Method, parsed.Method);
        Assert.Equal("echo", parsed.Params?.GetProperty("name").GetString());
    }

    [Fact]
    public void RoundTripsNotification()
    {
        var notification = new JsonRpcNotification
        {
            Method = "notifications/progress",
            Params = JsonSerializer.SerializeToElement(new { progress = 1 }),
        };

        var json = JsonSerializer.Serialize<JsonRpcMessage>(notification);
        var parsed = Assert.IsType<JsonRpcNotification>(JsonSerializer.Deserialize<JsonRpcMessage>(json));

        Assert.Equal(notification.Method, parsed.Method);
        Assert.Equal(1, parsed.Params?.GetProperty("progress").GetInt32());
    }

    [Fact]
    public void RoundTripsResultResponse()
    {
        var response = new JsonRpcResponse
        {
            Id = new JsonRpcId(3),
            Result = JsonSerializer.SerializeToElement(new { ok = true }),
        };

        var json = JsonSerializer.Serialize<JsonRpcMessage>(response);
        var parsed = Assert.IsType<JsonRpcResponse>(JsonSerializer.Deserialize<JsonRpcMessage>(json));

        Assert.Equal(response.Id, parsed.Id);
        Assert.True(parsed.Result?.GetProperty("ok").GetBoolean());
        Assert.Null(parsed.Error);
    }

    [Fact]
    public void RoundTripsErrorResponseWithNullId()
    {
        var response = new JsonRpcResponse
        {
            Id = null,
            Error = new JsonRpcError { Code = JsonRpcErrorCode.ParseError, Message = "bad json" },
        };

        var json = JsonSerializer.Serialize<JsonRpcMessage>(response);
        var parsed = Assert.IsType<JsonRpcResponse>(JsonSerializer.Deserialize<JsonRpcMessage>(json));

        Assert.Null(parsed.Id);
        Assert.Equal(JsonRpcErrorCode.ParseError, parsed.Error?.Code);
        Assert.Equal("bad json", parsed.Error?.Message);
    }

    [Fact]
    public void RejectsWrongProtocolVersion()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<JsonRpcMessage>("""{"jsonrpc":"1.0","id":1,"method":"x"}"""));
    }

    [Fact]
    public void RejectsUnknownMessage()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<JsonRpcMessage>("""{"jsonrpc":"2.0","version":"1"}"""));
    }

    [Fact]
    public void RoundTripsErrorData()
    {
        var response = new JsonRpcResponse
        {
            Id = new JsonRpcId(5),
            Error = new JsonRpcError
            {
                Code = -32000,
                Message = "denied",
                Data = JsonSerializer.SerializeToElement(new { reason = "policy" }),
            },
        };

        var json = JsonSerializer.Serialize<JsonRpcMessage>(response);
        var parsed = Assert.IsType<JsonRpcResponse>(JsonSerializer.Deserialize<JsonRpcMessage>(json));

        Assert.Equal("policy", parsed.Error?.Data?.GetProperty("reason").GetString());
    }
}

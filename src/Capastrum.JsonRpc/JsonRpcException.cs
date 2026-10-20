using System.Text.Json;

namespace Capastrum.JsonRpc;

/// <summary>
/// Represents an error response returned by a JSON-RPC peer.
/// </summary>
public sealed class JsonRpcException : Exception
{
    /// <summary>
    /// Creates an exception from a JSON-RPC error.
    /// </summary>
    public JsonRpcException(int code, string message, JsonElement? data = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Data = data;
    }

    /// <summary>
    /// The JSON-RPC error code.
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// Optional structured error details.
    /// </summary>
    public new JsonElement? Data { get; }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.JsonRpc;

/// <summary>
/// The base type of every JSON-RPC 2.0 message.
/// </summary>
[JsonConverter(typeof(JsonRpcMessageConverter))]
public abstract class JsonRpcMessage
{
    /// <summary>
    /// The JSON-RPC protocol version.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";
}

/// <summary>
/// A JSON-RPC request expecting a response.
/// </summary>
public sealed class JsonRpcRequest : JsonRpcMessage
{
    /// <summary>
    /// The request identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public required JsonRpcId Id { get; init; }

    /// <summary>
    /// The method name.
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>
    /// Optional method parameters.
    /// </summary>
    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Params { get; init; }
}

/// <summary>
/// A JSON-RPC notification that expects no response.
/// </summary>
public sealed class JsonRpcNotification : JsonRpcMessage
{
    /// <summary>
    /// The method name.
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>
    /// Optional notification parameters.
    /// </summary>
    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Params { get; init; }
}

/// <summary>
/// A JSON-RPC response carrying either a result or an error.
/// </summary>
public sealed class JsonRpcResponse : JsonRpcMessage
{
    /// <summary>
    /// The request identifier, or null when the request could not be identified.
    /// </summary>
    [JsonPropertyName("id")]
    public required JsonRpcId? Id { get; init; }

    /// <summary>
    /// The result of a successful request.
    /// </summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Result { get; init; }

    /// <summary>
    /// The error of a failed request.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; init; }

    /// <summary>
    /// True when the response carries an error.
    /// </summary>
    public bool IsError => Error is not null;
}

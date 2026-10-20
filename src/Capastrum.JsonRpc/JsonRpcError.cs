using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.JsonRpc;

/// <summary>
/// Describes a JSON-RPC error.
/// </summary>
public sealed class JsonRpcError
{
    /// <summary>
    /// The numeric error code.
    /// </summary>
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    /// <summary>
    /// A short human-readable description.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Optional structured error details.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Data { get; init; }
}

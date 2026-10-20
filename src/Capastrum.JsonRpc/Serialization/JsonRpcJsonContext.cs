using System.Text.Json.Serialization;

namespace Capastrum.JsonRpc.Serialization;

/// <summary>
/// Source-generated metadata for the JSON-RPC envelope.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JsonRpcMessage))]
[JsonSerializable(typeof(JsonRpcRequest))]
[JsonSerializable(typeof(JsonRpcResponse))]
[JsonSerializable(typeof(JsonRpcNotification))]
[JsonSerializable(typeof(JsonRpcError))]
public sealed partial class JsonRpcJsonContext : JsonSerializerContext;

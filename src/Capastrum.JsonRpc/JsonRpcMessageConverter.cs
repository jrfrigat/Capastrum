using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.JsonRpc;

/// <summary>
/// Reads and writes the polymorphic JSON-RPC message envelope.
/// </summary>
public sealed class JsonRpcMessageConverter : JsonConverter<JsonRpcMessage>
{
    /// <inheritdoc />
    public override JsonRpcMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A JSON-RPC message must be an object.");
        }

        if (!root.TryGetProperty("jsonrpc", out var version) || version.GetString() != "2.0")
        {
            throw new JsonException("A JSON-RPC message must declare jsonrpc 2.0.");
        }

        var hasMethod = root.TryGetProperty("method", out var methodElement)
            && methodElement.ValueKind == JsonValueKind.String;
        var hasId = root.TryGetProperty("id", out var idElement);

        if (hasMethod)
        {
            var method = methodElement.GetString()!;
            var parameters = root.TryGetProperty("params", out var paramsElement)
                ? paramsElement.Clone()
                : (JsonElement?)null;

            if (hasId && idElement.ValueKind != JsonValueKind.Null)
            {
                return new JsonRpcRequest
                {
                    Id = idElement.Deserialize<JsonRpcId>(options),
                    Method = method,
                    Params = parameters,
                };
            }

            return new JsonRpcNotification
            {
                Method = method,
                Params = parameters,
            };
        }

        if (hasId)
        {
            var id = idElement.ValueKind == JsonValueKind.Null
                ? (JsonRpcId?)null
                : idElement.Deserialize<JsonRpcId>(options);

            if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.Object)
            {
                return new JsonRpcResponse
                {
                    Id = id,
                    Error = ReadError(errorElement),
                };
            }

            var result = root.TryGetProperty("result", out var resultElement)
                ? resultElement.Clone()
                : (JsonElement?)null;

            return new JsonRpcResponse
            {
                Id = id,
                Result = result,
            };
        }

        throw new JsonException("The JSON-RPC message is neither a request, a notification nor a response.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, JsonRpcMessage value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteString("jsonrpc", "2.0");

        switch (value)
        {
            case JsonRpcRequest request:
                WriteId(writer, request.Id, options);
                writer.WriteString("method", request.Method);
                if (request.Params is { } parameters)
                {
                    writer.WritePropertyName("params");
                    parameters.WriteTo(writer);
                }

                break;

            case JsonRpcNotification notification:
                writer.WriteString("method", notification.Method);
                if (notification.Params is { } notificationParameters)
                {
                    writer.WritePropertyName("params");
                    notificationParameters.WriteTo(writer);
                }

                break;

            case JsonRpcResponse response:
                if (response.Id is { } responseId)
                {
                    WriteId(writer, responseId, options);
                }
                else
                {
                    writer.WriteNull("id");
                }

                if (response.Error is { } error)
                {
                    writer.WritePropertyName("error");
                    writer.WriteStartObject();
                    writer.WriteNumber("code", error.Code);
                    writer.WriteString("message", error.Message);
                    if (error.Data is { } errorData)
                    {
                        writer.WritePropertyName("data");
                        errorData.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                }
                else if (response.Result is { } result)
                {
                    writer.WritePropertyName("result");
                    result.WriteTo(writer);
                }
                else
                {
                    writer.WriteNull("result");
                }

                break;

            default:
                throw new JsonException($"Unknown JSON-RPC message type '{value.GetType()}'.");
        }

        writer.WriteEndObject();
    }

    private static void WriteId(Utf8JsonWriter writer, JsonRpcId id, JsonSerializerOptions options)
    {
        writer.WritePropertyName("id");
        JsonSerializer.Serialize(writer, id, options);
    }

    private static JsonRpcError ReadError(JsonElement element)
    {
        if (!element.TryGetProperty("code", out var codeElement) || !codeElement.TryGetInt32(out var code))
        {
            throw new JsonException("A JSON-RPC error must contain a numeric code.");
        }

        if (!element.TryGetProperty("message", out var messageElement) || messageElement.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("A JSON-RPC error must contain a message.");
        }

        var data = element.TryGetProperty("data", out var dataElement)
            ? dataElement.Clone()
            : (JsonElement?)null;

        return new JsonRpcError
        {
            Code = code,
            Message = messageElement.GetString()!,
            Data = data,
        };
    }
}

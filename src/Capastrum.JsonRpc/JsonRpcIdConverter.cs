using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.JsonRpc;

/// <summary>
/// Converts <see cref="JsonRpcId"/> to and from its number or string form.
/// </summary>
public sealed class JsonRpcIdConverter : JsonConverter<JsonRpcId>
{
    /// <inheritdoc />
    public override JsonRpcId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => new JsonRpcId(reader.GetString()!),
            JsonTokenType.Number => new JsonRpcId(reader.GetInt64()),
            _ => throw new JsonException("A JSON-RPC id must be a string or a number."),
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, JsonRpcId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value.IsNumber)
        {
            writer.WriteNumberValue(value.Number);
        }
        else
        {
            writer.WriteStringValue(value.Text);
        }
    }
}

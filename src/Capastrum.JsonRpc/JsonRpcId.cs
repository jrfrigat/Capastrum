using System.Globalization;
using System.Text.Json.Serialization;

namespace Capastrum.JsonRpc;

/// <summary>
/// Identifies a JSON-RPC request or response. An id is either a number or a string.
/// </summary>
[JsonConverter(typeof(JsonRpcIdConverter))]
public readonly struct JsonRpcId : IEquatable<JsonRpcId>
{
    private readonly long _number;
    private readonly string? _string;

    /// <summary>
    /// Creates a numeric id.
    /// </summary>
    public JsonRpcId(long value)
    {
        _number = value;
        _string = null;
    }

    /// <summary>
    /// Creates a string id.
    /// </summary>
    public JsonRpcId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _number = 0;
        _string = value;
    }

    /// <summary>
    /// True when the id is numeric.
    /// </summary>
    public bool IsNumber => _string is null;

    /// <summary>
    /// The numeric value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The id is a string.</exception>
    public long Number => IsNumber
        ? _number
        : throw new InvalidOperationException("The id is a string.");

    /// <summary>
    /// The string value, or null when the id is numeric.
    /// </summary>
    public string? Text => _string;

    /// <inheritdoc />
    public bool Equals(JsonRpcId other) => IsNumber
        ? other.IsNumber && _number == other._number
        : !other.IsNumber && string.Equals(_string, other._string, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is JsonRpcId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => IsNumber
        ? _number.GetHashCode()
        : StringComparer.Ordinal.GetHashCode(_string!);

    /// <inheritdoc />
    public override string ToString() => IsNumber
        ? _number.ToString(CultureInfo.InvariantCulture)
        : _string!;

    /// <summary>
    /// Compares two ids.
    /// </summary>
    public static bool operator ==(JsonRpcId left, JsonRpcId right) => left.Equals(right);

    /// <summary>
    /// Compares two ids.
    /// </summary>
    public static bool operator !=(JsonRpcId left, JsonRpcId right) => !left.Equals(right);
}

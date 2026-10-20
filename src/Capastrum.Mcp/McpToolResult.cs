using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Capastrum.Mcp.Protocol;

namespace Capastrum.Mcp;

/// <summary>
/// Factory methods for tool results.
/// </summary>
public static class McpToolResult
{
    /// <summary>Creates a text result.</summary>
    public static McpCallToolResult Text(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new McpCallToolResult
        {
            Content = new McpContentBlock[] { new McpTextContent { Text = text } },
        };
    }

    /// <summary>Creates a JSON result.</summary>
    public static McpCallToolResult Json(object? value)
    {
        var element = JsonSerializer.SerializeToElement(value);

        return new McpCallToolResult
        {
            Content = new McpContentBlock[] { new McpTextContent { Text = element.GetRawText() } },
            StructuredContent = element,
        };
    }

    /// <summary>Creates a JSON result using source-generated metadata.</summary>
    public static McpCallToolResult Json<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var element = JsonSerializer.SerializeToElement(value, typeInfo);

        return new McpCallToolResult
        {
            Content = new McpContentBlock[] { new McpTextContent { Text = element.GetRawText() } },
            StructuredContent = element,
        };
    }

    /// <summary>Creates an error result.</summary>
    public static McpCallToolResult Error(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new McpCallToolResult
        {
            Content = new McpContentBlock[] { new McpTextContent { Text = message } },
            IsError = true,
        };
    }

    /// <summary>Creates an input-required result with one request.</summary>
    public static McpInputRequiredResult InputRequired(
        string inputId,
        McpInputRequest request,
        string? requestState = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputId);
        ArgumentNullException.ThrowIfNull(request);

        return new McpInputRequiredResult
        {
            InputRequests = new Dictionary<string, McpInputRequest> { [inputId] = request },
            RequestState = requestState,
        };
    }

    /// <summary>Creates an input-required result with several requests.</summary>
    public static McpInputRequiredResult InputRequired(
        IReadOnlyDictionary<string, McpInputRequest> requests,
        string? requestState = null)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return new McpInputRequiredResult
        {
            InputRequests = new Dictionary<string, McpInputRequest>(requests),
            RequestState = requestState,
        };
    }
}

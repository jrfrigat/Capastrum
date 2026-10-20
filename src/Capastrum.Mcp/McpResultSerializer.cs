using System.Text.Json;
using Capastrum.Mcp.Protocol;
using Capastrum.Mcp.Protocol.Serialization;

namespace Capastrum.Mcp;

internal static class McpResultSerializer
{
    public static JsonElement Serialize(McpResult result) => result switch
    {
        McpDiscoverResult discover => JsonSerializer.SerializeToElement(
            discover,
            McpJsonContext.Default.McpDiscoverResult),
        McpListToolsResult tools => JsonSerializer.SerializeToElement(
            tools,
            McpJsonContext.Default.McpListToolsResult),
        McpCallToolResult call => JsonSerializer.SerializeToElement(
            call,
            McpJsonContext.Default.McpCallToolResult),
        McpListResourcesResult resources => JsonSerializer.SerializeToElement(
            resources,
            McpJsonContext.Default.McpListResourcesResult),
        McpReadResourceResult read => JsonSerializer.SerializeToElement(
            read,
            McpJsonContext.Default.McpReadResourceResult),
        McpListResourceTemplatesResult templates => JsonSerializer.SerializeToElement(
            templates,
            McpJsonContext.Default.McpListResourceTemplatesResult),
        McpListPromptsResult prompts => JsonSerializer.SerializeToElement(
            prompts,
            McpJsonContext.Default.McpListPromptsResult),
        McpGetPromptResult prompt => JsonSerializer.SerializeToElement(
            prompt,
            McpJsonContext.Default.McpGetPromptResult),
        McpCompleteResult completion => JsonSerializer.SerializeToElement(
            completion,
            McpJsonContext.Default.McpCompleteResult),
        McpInputRequiredResult input => JsonSerializer.SerializeToElement(
            input,
            McpJsonContext.Default.McpInputRequiredResult),
        _ => throw new InvalidOperationException($"Unsupported MCP result type '{result.GetType()}'."),
    };
}

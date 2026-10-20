using Capastrum.Mcp;
using Capastrum.Mcp.Protocol;

var server = new McpServer(new McpServerOptions
{
    Name = "Capastrum.McpServerSample",
    Version = "1.0.0",
    Instructions = "A minimal MCP server that exposes an echo tool.",
});

server.RegisterTool(
    "echo",
    "Echoes the supplied value.",
    (context, _) => ValueTask.FromResult<McpResult>(
        McpToolResult.Text(context.Arguments.GetProperty("value").GetString() ?? string.Empty)));

await server.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());

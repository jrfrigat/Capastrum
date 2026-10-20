using System.Text.Json;
using Capastrum.JsonRpc;
using Capastrum.Mcp.Protocol;
using Capastrum.Mcp.Protocol.Serialization;
using Xunit;

namespace Capastrum.Mcp.Tests;

public sealed class McpContractTests
{
    private static McpRequestMeta CreateMeta() => new()
    {
        ProtocolVersion = McpProtocol.Version,
        ClientCapabilities = new McpClientCapabilities
        {
            Sampling = JsonSerializer.SerializeToElement(new { }),
        },
    };

    [Fact]
    public void DiscoverRoundTrips()
    {
        var result = new McpDiscoverResult
        {
            SupportedVersions = new[] { McpProtocol.Version },
            Capabilities = new McpServerCapabilities
            {
                Tools = JsonSerializer.SerializeToElement(new { listChanged = true }),
            },
            Instructions = "test server",
            TtlMs = 0,
            CacheScope = McpCacheScope.Public,
        };

        var json = JsonSerializer.Serialize(result, McpJsonContext.Default.McpDiscoverResult);
        var parsed = JsonSerializer.Deserialize(json, McpJsonContext.Default.McpDiscoverResult);

        Assert.Contains("\"resultType\":\"complete\"", json, StringComparison.Ordinal);
        Assert.Contains("\"cacheScope\":\"public\"", json, StringComparison.Ordinal);
        Assert.Equal(new[] { McpProtocol.Version }, parsed!.SupportedVersions);
        Assert.Equal(McpCacheScope.Public, parsed.CacheScope);
        Assert.True(parsed.Capabilities.Tools?.GetProperty("listChanged").GetBoolean());
    }

    [Fact]
    public void RequestMetaUsesProtocolKeys()
    {
        var request = new McpCallToolRequestParams
        {
            Meta = CreateMeta(),
            Name = "echo",
            Arguments = JsonSerializer.SerializeToElement(new { value = 42 }),
        };

        var json = JsonSerializer.Serialize(request, McpJsonContext.Default.McpCallToolRequestParams);

        Assert.Contains("\"io.modelcontextprotocol/protocolVersion\":\"2026-07-28\"", json, StringComparison.Ordinal);
        Assert.Contains("\"io.modelcontextprotocol/clientCapabilities\":", json, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"echo\"", json, StringComparison.Ordinal);
        Assert.Contains("\"arguments\":{\"value\":42}", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolResultRoundTripsContentBlocks()
    {
        var result = new McpCallToolResult
        {
            Content = new McpContentBlock[]
            {
                new McpTextContent { Text = "hello" },
                new McpImageContent { Data = "AA==", MimeType = "image/png" },
                new McpResourceLink { Uri = "file:///a.txt", Name = "a" },
                new McpEmbeddedResource
                {
                    Resource = new McpResourceContents { Uri = "file:///a.txt", Text = "body" },
                },
            },
        };

        var json = JsonSerializer.Serialize(result, McpJsonContext.Default.McpCallToolResult);
        var parsed = JsonSerializer.Deserialize(json, McpJsonContext.Default.McpCallToolResult);

        Assert.IsType<McpTextContent>(parsed!.Content[0]);
        Assert.IsType<McpImageContent>(parsed.Content[1]);
        Assert.IsType<McpResourceLink>(parsed.Content[2]);
        var embedded = Assert.IsType<McpEmbeddedResource>(parsed.Content[3]);
        Assert.Equal("body", embedded.Resource.Text);
    }

    [Fact]
    public void InputRequiredRoundTripsTypedRequests()
    {
        var result = new McpInputRequiredResult
        {
            InputRequests = new Dictionary<string, McpInputRequest>
            {
                ["sampling"] = new McpCreateMessageInputRequest
                {
                    Params = new McpCreateMessageRequestParams
                    {
                        MaxTokens = 10,
                        Messages = new[]
                        {
                            new McpSamplingMessage
                            {
                                Role = McpRole.User,
                                Content = JsonSerializer.SerializeToElement(new { type = "text", text = "hi" }),
                            },
                        },
                    },
                },
                ["roots"] = new McpListRootsInputRequest(),
                ["elicit"] = new McpElicitInputRequest
                {
                    Params = new McpElicitFormRequestParams
                    {
                        Message = "name?",
                        RequestedSchema = JsonSerializer.SerializeToElement(new { type = "object" }),
                    },
                },
            },
        };

        var json = JsonSerializer.Serialize(result, McpJsonContext.Default.McpInputRequiredResult);
        var parsed = JsonSerializer.Deserialize(json, McpJsonContext.Default.McpInputRequiredResult);

        Assert.Contains("\"resultType\":\"input_required\"", json, StringComparison.Ordinal);
        Assert.Contains("\"method\":\"sampling/createMessage\"", json, StringComparison.Ordinal);
        Assert.IsType<McpCreateMessageInputRequest>(parsed!.InputRequests!["sampling"]);
        Assert.IsType<McpListRootsInputRequest>(parsed.InputRequests["roots"]);
        var elicit = Assert.IsType<McpElicitInputRequest>(parsed.InputRequests["elicit"]);
        Assert.IsType<McpElicitFormRequestParams>(elicit.Params);
    }

    [Fact]
    public void CompletionReferencesUseDiscriminators()
    {
        var prompt = new McpCompleteRequestParams
        {
            Meta = CreateMeta(),
            Ref = new McpPromptReference { Name = "summarize" },
            Argument = new McpCompletionArgument { Name = "topic", Value = "x" },
        };

        var promptJson = JsonSerializer.Serialize(prompt, McpJsonContext.Default.McpCompleteRequestParams);
        Assert.Contains("\"type\":\"ref/prompt\"", promptJson, StringComparison.Ordinal);

        var resource = new McpCompleteRequestParams
        {
            Meta = CreateMeta(),
            Ref = new McpResourceTemplateReference { Uri = "file:///{path}" },
            Argument = new McpCompletionArgument { Name = "path", Value = "a" },
        };

        var resourceJson = JsonSerializer.Serialize(resource, McpJsonContext.Default.McpCompleteRequestParams);
        Assert.Contains("\"type\":\"ref/resource\"", resourceJson, StringComparison.Ordinal);
    }

    [Fact]
    public void SubscriptionResultCarriesSubscriptionId()
    {
        var result = new McpSubscriptionsListenResult
        {
            Meta = new McpSubscriptionsListenResultMeta
            {
                SubscriptionId = new JsonRpcId(7),
            },
        };

        var json = JsonSerializer.Serialize(result, McpJsonContext.Default.McpSubscriptionsListenResult);

        Assert.Contains("\"io.modelcontextprotocol/subscriptionId\":7", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgressNotificationRoundTrips()
    {
        var notification = new McpProgressNotificationParams
        {
            ProgressToken = new JsonRpcId("token"),
            Progress = 0.5,
            Total = 1,
            Message = "half",
        };

        var json = JsonSerializer.Serialize(notification, McpJsonContext.Default.McpProgressNotificationParams);
        var parsed = JsonSerializer.Deserialize(json, McpJsonContext.Default.McpProgressNotificationParams);

        Assert.Equal(new JsonRpcId("token"), parsed!.ProgressToken);
        Assert.Equal(0.5, parsed.Progress);
        Assert.Equal("half", parsed.Message);
    }

    [Fact]
    public void ToolAndListResultRoundTrip()
    {
        var tool = new McpTool
        {
            Name = "read_kanban_board",
            Description = "Returns active cards",
            InputSchema = JsonSerializer.SerializeToElement(new { type = "object" }),
        };

        var result = new McpListToolsResult
        {
            Tools = new[] { tool },
            TtlMs = 1000,
            CacheScope = McpCacheScope.Private,
        };

        var json = JsonSerializer.Serialize(result, McpJsonContext.Default.McpListToolsResult);
        var parsed = JsonSerializer.Deserialize(json, McpJsonContext.Default.McpListToolsResult);

        Assert.Equal("read_kanban_board", parsed!.Tools[0].Name);
        Assert.Equal("object", parsed.Tools[0].InputSchema.GetProperty("type").GetString());
        Assert.Equal(McpCacheScope.Private, parsed.CacheScope);
    }
}

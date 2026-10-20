using System.Text.Json;
using Capastrum.Acp.Protocol;
using Capastrum.Acp.Protocol.Serialization;
using Xunit;

namespace Capastrum.Acp.Tests;

public sealed class AcpContractTests
{
    [Fact]
    public void InitializeRoundTrips()
    {
        var request = new AcpInitializeRequest
        {
            ProtocolVersion = AcpProtocol.Version,
            ClientCapabilities = new AcpClientCapabilities
            {
                FileSystem = new AcpFileSystemCapabilities
                {
                    ReadTextFile = true,
                    WriteTextFile = true,
                },
                Terminal = true,
            },
            ClientInfo = new AcpImplementation { Name = "client", Version = "1.0.0" },
        };

        var json = JsonSerializer.Serialize(request, AcpJsonContext.Default.AcpInitializeRequest);
        var parsed = JsonSerializer.Deserialize(json, AcpJsonContext.Default.AcpInitializeRequest);

        Assert.Contains("\"protocolVersion\":1", json, StringComparison.Ordinal);
        Assert.Equal(1, parsed!.ProtocolVersion);
        Assert.True(parsed.ClientCapabilities?.FileSystem?.ReadTextFile);
        Assert.Equal("client", parsed.ClientInfo?.Name);
    }

    [Fact]
    public void SessionConfigOptionsUseDiscriminators()
    {
        var response = new AcpNewSessionResponse
        {
            SessionId = "session-1",
            Modes = new AcpSessionModeState
            {
                CurrentModeId = "code",
                AvailableModes = new[]
                {
                    new AcpSessionMode { Id = "code", Name = "Code" },
                },
            },
            ConfigOptions = new AcpSessionConfigOption[]
            {
                new AcpSessionConfigSelect
                {
                    Id = "model",
                    Name = "Model",
                    CurrentValue = "a",
                    Options = JsonSerializer.SerializeToElement(new[] { new { value = "a", name = "A" } }),
                },
                new AcpSessionConfigBoolean
                {
                    Id = "fast",
                    Name = "Fast",
                    CurrentValue = true,
                },
            },
        };

        var json = JsonSerializer.Serialize(response, AcpJsonContext.Default.AcpNewSessionResponse);
        var parsed = JsonSerializer.Deserialize(json, AcpJsonContext.Default.AcpNewSessionResponse);

        Assert.Contains("\"type\":\"select\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"boolean\"", json, StringComparison.Ordinal);
        Assert.IsType<AcpSessionConfigSelect>(parsed!.ConfigOptions![0]);
        Assert.IsType<AcpSessionConfigBoolean>(parsed.ConfigOptions[1]);
    }

    [Fact]
    public void PromptRoundTripsContentBlocks()
    {
        var request = new AcpPromptRequest
        {
            SessionId = "session-1",
            Prompt = new AcpContentBlock[]
            {
                new AcpTextContent { Text = "hello" },
                new AcpImageContent { Data = "AA==", MimeType = "image/png" },
                new AcpResourceLink { Name = "a", Uri = "file:///a" },
            },
        };

        var json = JsonSerializer.Serialize(request, AcpJsonContext.Default.AcpPromptRequest);
        var parsed = JsonSerializer.Deserialize(json, AcpJsonContext.Default.AcpPromptRequest);

        Assert.Contains("\"type\":\"text\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"image\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"resource_link\"", json, StringComparison.Ordinal);
        Assert.IsType<AcpTextContent>(parsed!.Prompt[0]);
        Assert.IsType<AcpImageContent>(parsed.Prompt[1]);
        Assert.IsType<AcpResourceLink>(parsed.Prompt[2]);
    }

    [Fact]
    public void SessionUpdatesUseSessionUpdateDiscriminator()
    {
        var notification = new AcpSessionNotification
        {
            SessionId = "session-1",
            Update = new AcpAgentMessageChunk
            {
                Content = new AcpTextContent { Text = "hi" },
                MessageId = "message-1",
            },
        };

        var json = JsonSerializer.Serialize(notification, AcpJsonContext.Default.AcpSessionNotification);
        var parsed = JsonSerializer.Deserialize(json, AcpJsonContext.Default.AcpSessionNotification);

        Assert.Contains("\"sessionUpdate\":\"agent_message_chunk\"", json, StringComparison.Ordinal);
        var chunk = Assert.IsType<AcpAgentMessageChunk>(parsed!.Update);
        Assert.Equal("message-1", chunk.MessageId);
        Assert.Equal("hi", Assert.IsType<AcpTextContent>(chunk.Content).Text);
    }

    [Fact]
    public void ToolCallAndPlanUpdatesRoundTrip()
    {
        var toolCall = new AcpSessionNotification
        {
            SessionId = "s",
            Update = new AcpToolCallNotification
            {
                ToolCallId = "tool-1",
                Title = "Read file",
                Kind = AcpToolKind.Read,
                Status = AcpToolCallStatus.Pending,
                Content = new AcpToolCallContent[]
                {
                    new AcpContentToolCallContent
                    {
                        Content = new AcpTextContent { Text = "content" },
                    },
                },
            },
        };

        var toolJson = JsonSerializer.Serialize(toolCall, AcpJsonContext.Default.AcpSessionNotification);
        var parsedTool = JsonSerializer.Deserialize(toolJson, AcpJsonContext.Default.AcpSessionNotification);
        Assert.Contains("\"sessionUpdate\":\"tool_call\"", toolJson, StringComparison.Ordinal);
        Assert.IsType<AcpToolCallNotification>(parsedTool!.Update);

        var plan = new AcpSessionNotification
        {
            SessionId = "s",
            Update = new AcpPlanUpdate
            {
                Entries = new[]
                {
                    new AcpPlanEntry
                    {
                        Content = "step",
                        Priority = AcpPlanEntryPriority.High,
                        Status = AcpPlanEntryStatus.Pending,
                    },
                },
            },
        };

        var planJson = JsonSerializer.Serialize(plan, AcpJsonContext.Default.AcpSessionNotification);
        Assert.Contains("\"sessionUpdate\":\"plan\"", planJson, StringComparison.Ordinal);

        var usage = new AcpSessionNotification
        {
            SessionId = "s",
            Update = new AcpUsageUpdate { Used = 10, Size = 100 },
        };

        var usageJson = JsonSerializer.Serialize(usage, AcpJsonContext.Default.AcpSessionNotification);
        Assert.Contains("\"sessionUpdate\":\"usage_update\"", usageJson, StringComparison.Ordinal);
    }

    [Fact]
    public void PermissionOutcomesUseOutcomeDiscriminator()
    {
        var selected = new AcpRequestPermissionResponse
        {
            Outcome = new AcpSelectedPermissionOutcome { OptionId = "allow" },
        };

        var selectedJson = JsonSerializer.Serialize(selected, AcpJsonContext.Default.AcpRequestPermissionResponse);
        var parsedSelected = JsonSerializer.Deserialize(selectedJson, AcpJsonContext.Default.AcpRequestPermissionResponse);

        Assert.Contains("\"outcome\":\"selected\"", selectedJson, StringComparison.Ordinal);
        Assert.IsType<AcpSelectedPermissionOutcome>(parsedSelected!.Outcome);

        var cancelled = new AcpRequestPermissionResponse
        {
            Outcome = new AcpCancelledPermissionOutcome(),
        };

        var cancelledJson = JsonSerializer.Serialize(cancelled, AcpJsonContext.Default.AcpRequestPermissionResponse);
        Assert.Contains("\"outcome\":\"cancelled\"", cancelledJson, StringComparison.Ordinal);
    }

    [Fact]
    public void TerminalAndFileRequestsRoundTrip()
    {
        var create = new AcpCreateTerminalRequest
        {
            SessionId = "s",
            Command = "ls",
            Args = new[] { "-la" },
            Environment = new[] { new AcpEnvVariable { Name = "A", Value = "B" } },
            Cwd = "/tmp",
        };

        var createJson = JsonSerializer.Serialize(create, AcpJsonContext.Default.AcpCreateTerminalRequest);
        var parsedCreate = JsonSerializer.Deserialize(createJson, AcpJsonContext.Default.AcpCreateTerminalRequest);

        Assert.Equal("ls", parsedCreate!.Command);
        Assert.Equal("A", parsedCreate.Environment![0].Name);

        var read = new AcpReadTextFileRequest
        {
            SessionId = "s",
            Path = "/tmp/a.txt",
            Line = 1,
            Limit = 10,
        };

        var readJson = JsonSerializer.Serialize(read, AcpJsonContext.Default.AcpReadTextFileRequest);
        var parsedRead = JsonSerializer.Deserialize(readJson, AcpJsonContext.Default.AcpReadTextFileRequest);

        Assert.Equal("/tmp/a.txt", parsedRead!.Path);
        Assert.Equal(1, parsedRead.Line);
        Assert.Equal(10, parsedRead.Limit);
    }
}

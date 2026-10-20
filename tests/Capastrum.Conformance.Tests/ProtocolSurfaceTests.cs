using System.Reflection;
using Capastrum.Acp.Protocol;
using Capastrum.Mcp.Protocol;
using Xunit;

namespace Capastrum.Conformance.Tests;

public sealed class ProtocolSurfaceTests
{
    [Fact]
    public void McpMethodTablesAreComplete()
    {
        AssertConstantTable(typeof(McpProtocol.Methods));
        AssertConstantTable(typeof(McpProtocol.Notifications));
    }

    [Fact]
    public void AcpMethodTablesAreComplete()
    {
        AssertConstantTable(typeof(AcpProtocol.Methods));
        AssertConstantTable(typeof(AcpProtocol.Notifications));
    }

    private static void AssertConstantTable(Type type)
    {
        var values = type
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        Assert.NotEmpty(values);
        Assert.DoesNotContain(values, static value => string.IsNullOrWhiteSpace(value));
        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
    }
}

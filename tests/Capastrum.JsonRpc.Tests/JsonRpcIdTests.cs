using System.Text.Json;
using Xunit;

namespace Capastrum.JsonRpc.Tests;

public sealed class JsonRpcIdTests
{
    [Fact]
    public void NumericAndStringIdsAreDistinct()
    {
        var number = new JsonRpcId(42);
        var text = new JsonRpcId("42");

        Assert.NotEqual(number, text);
        Assert.True(number.IsNumber);
        Assert.False(text.IsNumber);
        Assert.Equal("42", number.ToString());
        Assert.Equal("42", text.ToString());
        Assert.Equal("42", text.Text);
    }

    [Fact]
    public void SerializesNumbersAsNumbersAndStringsAsStrings()
    {
        Assert.Equal("42", JsonSerializer.Serialize(new JsonRpcId(42)));
        Assert.Equal("\"42\"", JsonSerializer.Serialize(new JsonRpcId("42")));
    }

    [Fact]
    public void RoundTripsBothForms()
    {
        Assert.Equal(new JsonRpcId(7), JsonSerializer.Deserialize<JsonRpcId>("7"));
        Assert.Equal(new JsonRpcId("seven"), JsonSerializer.Deserialize<JsonRpcId>("\"seven\""));
    }
}

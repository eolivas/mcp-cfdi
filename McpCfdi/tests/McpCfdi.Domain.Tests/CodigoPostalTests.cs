using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class CodigoPostalTests
{
    [Theory]
    [InlineData("06600")]
    [InlineData("00000")]
    [InlineData("99999")]
    [InlineData("12345")]
    public void Constructor_WithValid5Digits_CreatesInstance(string valor)
    {
        var cp = new CodigoPostal(valor);

        Assert.Equal(valor, cp.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("abcde")]
    [InlineData("1234a")]
    [InlineData("12 34")]
    [InlineData(" 12345")]
    [InlineData("12345 ")]
    public void Constructor_WithInvalidValue_ThrowsArgumentException(string valor)
    {
        Assert.Throws<ArgumentException>(() => new CodigoPostal(valor));
    }

    [Fact]
    public void Constructor_WithNull_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CodigoPostal(null!));
    }

    [Fact]
    public void Equality_SameValor_AreEqual()
    {
        var a = new CodigoPostal("06600");
        var b = new CodigoPostal("06600");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValor_AreNotEqual()
    {
        var a = new CodigoPostal("06600");
        var b = new CodigoPostal("01000");

        Assert.NotEqual(a, b);
    }
}

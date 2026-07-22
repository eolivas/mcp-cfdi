using McpCfdi.Domain.Exceptions;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class MontoMonedaTests
{
    // --- Construction and Rounding ---

    [Fact]
    public void Constructor_WithValidValues_CreatesInstance()
    {
        var monto = new MontoMoneda(100.50m, 2);

        Assert.Equal(100.50m, monto.Valor);
        Assert.Equal(2, monto.Decimales);
    }

    [Fact]
    public void Constructor_WithZeroValue_CreatesInstance()
    {
        var monto = new MontoMoneda(0m, 2);

        Assert.Equal(0m, monto.Valor);
    }

    [Theory]
    [InlineData("10.555", 2, "10.56")] // MidpointRounding.AwayFromZero rounds .5 up
    [InlineData("10.545", 2, "10.55")]
    [InlineData("10.5450", 2, "10.55")]
    [InlineData("1.005", 2, "1.01")]   // classic banker's rounding difference
    [InlineData("99.9999", 2, "100.00")]
    [InlineData("1.23456789", 4, "1.2346")]
    [InlineData("5.5", 0, "6")]
    [InlineData("4.5", 0, "5")]        // AwayFromZero: .5 rounds up
    public void Constructor_RoundsWithMidpointAwayFromZero(string inputStr, int decimales, string expectedStr)
    {
        var input = decimal.Parse(inputStr);
        var expected = decimal.Parse(expectedStr);

        var monto = new MontoMoneda(input, decimales);

        Assert.Equal(expected, monto.Valor);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-1")]
    [InlineData("-100.50")]
    public void Constructor_WithNegativeValue_ThrowsInvalidMontoException(string valorStr)
    {
        var valor = decimal.Parse(valorStr);

        var ex = Assert.Throws<InvalidMontoException>(() => new MontoMoneda(valor, 2));

        Assert.Equal(valor, ex.ValorInvalido);
    }

    // --- Addition Operator ---

    [Fact]
    public void Addition_SameDecimals_AddsCorrectly()
    {
        var a = new MontoMoneda(10.50m, 2);
        var b = new MontoMoneda(20.75m, 2);

        var result = a + b;

        Assert.Equal(31.25m, result.Valor);
        Assert.Equal(2, result.Decimales);
    }

    [Fact]
    public void Addition_ResultIsRounded()
    {
        // 10.555 rounds to 10.56, 20.444 rounds to 20.44 => sum of rounded = 30.996 rounds to 31.00
        var a = new MontoMoneda(10.555m, 2); // stored as 10.56
        var b = new MontoMoneda(20.444m, 2); // stored as 20.44

        var result = a + b;

        Assert.Equal(31.00m, result.Valor);
    }

    [Fact]
    public void Addition_DifferentDecimals_ThrowsInvalidOperationException()
    {
        var a = new MontoMoneda(10m, 2);
        var b = new MontoMoneda(20m, 4);

        Assert.Throws<InvalidOperationException>(() => a + b);
    }

    // --- Multiplication Operator ---

    [Fact]
    public void Multiplication_MultipliesAndRounds()
    {
        var monto = new MontoMoneda(100m, 2);

        var result = monto * 0.165m; // 16.5 → rounds to 16.50

        Assert.Equal(16.50m, result.Valor);
    }

    [Fact]
    public void Multiplication_RoundsResultAwayFromZero()
    {
        var monto = new MontoMoneda(10m, 2);

        var result = monto * 0.335m; // 3.35 exactly

        Assert.Equal(3.35m, result.Valor);
    }

    [Fact]
    public void Multiplication_PreservesDecimals()
    {
        var monto = new MontoMoneda(50m, 4);

        var result = monto * 2m;

        Assert.Equal(4, result.Decimales);
    }

    [Fact]
    public void Multiplication_WithNegativeFactor_ThrowsInvalidMontoException()
    {
        var monto = new MontoMoneda(100m, 2);

        Assert.Throws<InvalidMontoException>(() => monto * -0.5m);
    }

    // --- FormatearParaXml ---

    [Theory]
    [InlineData("100", 2, "100.00")]
    [InlineData("100", 0, "100")]
    [InlineData("99.5", 2, "99.50")]
    [InlineData("1234.5678", 4, "1234.5678")]
    [InlineData("0", 2, "0.00")]
    [InlineData("0.1", 6, "0.100000")]
    public void FormatearParaXml_ReturnsExactDecimalPlaces(string valorStr, int decimales, string expected)
    {
        var valor = decimal.Parse(valorStr);
        var monto = new MontoMoneda(valor, decimales);

        Assert.Equal(expected, monto.FormatearParaXml());
    }

    // --- Record Equality ---

    [Fact]
    public void Equality_SameValorAndDecimals_AreEqual()
    {
        var a = new MontoMoneda(100.50m, 2);
        var b = new MontoMoneda(100.50m, 2);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValor_AreNotEqual()
    {
        var a = new MontoMoneda(100m, 2);
        var b = new MontoMoneda(200m, 2);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_DifferentDecimals_AreNotEqual()
    {
        var a = new MontoMoneda(100m, 2);
        var b = new MontoMoneda(100m, 4);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_RoundedToSameValue_AreEqual()
    {
        var a = new MontoMoneda(10.555m, 2); // rounds to 10.56
        var b = new MontoMoneda(10.56m, 2);

        Assert.Equal(a, b);
    }
}

using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class ClaveCatalogoTests
{
    [Fact]
    public void Constructor_WithValidArgs_CreatesClaveCatalogo()
    {
        var clave = new ClaveCatalogo("c_Moneda", "MXN");

        Assert.Equal("c_Moneda", clave.Catalogo);
        Assert.Equal("MXN", clave.Clave);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyCatalogo_ThrowsArgumentException(string? catalogo)
    {
        Assert.Throws<ArgumentException>(() => new ClaveCatalogo(catalogo!, "001"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyClave_ThrowsArgumentException(string? clave)
    {
        Assert.Throws<ArgumentException>(() => new ClaveCatalogo("c_FormaPago", clave!));
    }

    [Fact]
    public void ToString_ReturnsCatalogoColonClave()
    {
        var clave = new ClaveCatalogo("c_UsoCFDI", "G03");

        Assert.Equal("c_UsoCFDI:G03", clave.ToString());
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new ClaveCatalogo("c_Moneda", "USD");
        var b = new ClaveCatalogo("c_Moneda", "USD");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new ClaveCatalogo("c_Moneda", "USD");
        var b = new ClaveCatalogo("c_Moneda", "MXN");

        Assert.NotEqual(a, b);
    }
}

using McpCfdi.Domain.Entities;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class EmisorTests
{
    private static Rfc ValidRfc() => new("AAA010101AAA");
    private static ClaveCatalogo ValidRegimen() => new("c_RegimenFiscal", "601");

    [Fact]
    public void Constructor_WithValidArgs_CreatesEmisor()
    {
        var rfc = ValidRfc();
        var regimen = ValidRegimen();

        var emisor = new Emisor(rfc, "Empresa SA de CV", regimen);

        Assert.Equal(rfc, emisor.Rfc);
        Assert.Equal("Empresa SA de CV", emisor.Nombre);
        Assert.Equal(regimen, emisor.RegimenFiscal);
        Assert.NotEqual(Guid.Empty, emisor.Id);
    }

    [Fact]
    public void Constructor_TrimsNombre()
    {
        var emisor = new Emisor(ValidRfc(), "  Empresa SA de CV  ", ValidRegimen());

        Assert.Equal("Empresa SA de CV", emisor.Nombre);
    }

    [Fact]
    public void Constructor_WithSingleCharNombre_Succeeds()
    {
        var emisor = new Emisor(ValidRfc(), "A", ValidRegimen());

        Assert.Equal("A", emisor.Nombre);
    }

    [Fact]
    public void Constructor_WithMaxLengthNombre_Succeeds()
    {
        var nombre = new string('A', 254);

        var emisor = new Emisor(ValidRfc(), nombre, ValidRegimen());

        Assert.Equal(254, emisor.Nombre.Length);
    }

    [Fact]
    public void Constructor_WithNombreExceeding254Chars_ThrowsArgumentException()
    {
        var nombre = new string('A', 255);

        var ex = Assert.Throws<ArgumentException>(() => new Emisor(ValidRfc(), nombre, ValidRegimen()));
        Assert.Equal("nombre", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyNombre_ThrowsArgumentException(string? nombre)
    {
        var ex = Assert.Throws<ArgumentException>(() => new Emisor(ValidRfc(), nombre!, ValidRegimen()));
        Assert.Equal("nombre", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRfc_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Emisor(null!, "Empresa", ValidRegimen()));
        Assert.Equal("rfc", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRegimenFiscal_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Emisor(ValidRfc(), "Empresa", null!));
        Assert.Equal("regimenFiscal", ex.ParamName);
    }
}

using McpCfdi.Domain.Entities;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class ReceptorTests
{
    private static Rfc ValidRfc() => new("XAXX010101000");
    private static CodigoPostal ValidCodigoPostal() => new("06600");
    private static ClaveCatalogo ValidRegimenFiscal() => new("c_RegimenFiscal", "601");
    private static ClaveCatalogo ValidUsoCfdi() => new("c_UsoCFDI", "G03");

    [Fact]
    public void Constructor_WithValidArgs_CreatesReceptor()
    {
        var rfc = ValidRfc();
        var cp = ValidCodigoPostal();
        var regimen = ValidRegimenFiscal();
        var uso = ValidUsoCfdi();

        var receptor = new Receptor(rfc, "Cliente SA de CV", cp, regimen, uso);

        Assert.Equal(rfc, receptor.Rfc);
        Assert.Equal("Cliente SA de CV", receptor.Nombre);
        Assert.Equal(cp, receptor.DomicilioFiscalReceptor);
        Assert.Equal(regimen, receptor.RegimenFiscalReceptor);
        Assert.Equal(uso, receptor.UsoCfdi);
        Assert.NotEqual(Guid.Empty, receptor.Id);
    }

    [Fact]
    public void Constructor_TrimsNombre()
    {
        var receptor = new Receptor(ValidRfc(), "  Cliente SA de CV  ", ValidCodigoPostal(), ValidRegimenFiscal(), ValidUsoCfdi());

        Assert.Equal("Cliente SA de CV", receptor.Nombre);
    }

    [Fact]
    public void Constructor_WithSingleCharNombre_Succeeds()
    {
        var receptor = new Receptor(ValidRfc(), "A", ValidCodigoPostal(), ValidRegimenFiscal(), ValidUsoCfdi());

        Assert.Equal("A", receptor.Nombre);
    }

    [Fact]
    public void Constructor_WithMaxLengthNombre_Succeeds()
    {
        var nombre = new string('A', 254);

        var receptor = new Receptor(ValidRfc(), nombre, ValidCodigoPostal(), ValidRegimenFiscal(), ValidUsoCfdi());

        Assert.Equal(254, receptor.Nombre.Length);
    }

    [Fact]
    public void Constructor_WithNombreExceeding254Chars_ThrowsArgumentException()
    {
        var nombre = new string('A', 255);

        var ex = Assert.Throws<ArgumentException>(() =>
            new Receptor(ValidRfc(), nombre, ValidCodigoPostal(), ValidRegimenFiscal(), ValidUsoCfdi()));
        Assert.Equal("nombre", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyNombre_ThrowsArgumentException(string? nombre)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Receptor(ValidRfc(), nombre!, ValidCodigoPostal(), ValidRegimenFiscal(), ValidUsoCfdi()));
        Assert.Equal("nombre", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRfc_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Receptor(null!, "Cliente", ValidCodigoPostal(), ValidRegimenFiscal(), ValidUsoCfdi()));
        Assert.Equal("rfc", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDomicilioFiscal_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Receptor(ValidRfc(), "Cliente", null!, ValidRegimenFiscal(), ValidUsoCfdi()));
        Assert.Equal("domicilioFiscalReceptor", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRegimenFiscal_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Receptor(ValidRfc(), "Cliente", ValidCodigoPostal(), null!, ValidUsoCfdi()));
        Assert.Equal("regimenFiscalReceptor", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullUsoCfdi_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Receptor(ValidRfc(), "Cliente", ValidCodigoPostal(), ValidRegimenFiscal(), null!));
        Assert.Equal("usoCfdi", ex.ParamName);
    }
}

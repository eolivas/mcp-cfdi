using McpCfdi.Domain.Entities;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class TrasladoConceptoTests
{
    private static MontoMoneda CreateBase() => new(100m, 2);
    private static MontoMoneda CreateImporte() => new(16m, 2);
    private static ClaveCatalogo CreateImpuesto() => new("c_Impuesto", "002");
    private static ClaveCatalogo CreateTipoFactor(string clave) => new("c_TipoFactor", clave);

    // --- Valid Construction: Tasa ---

    [Fact]
    public void Constructor_WithTasa_CreatesInstanceWithAllFields()
    {
        var @base = CreateBase();
        var impuesto = CreateImpuesto();
        var tipoFactor = CreateTipoFactor("Tasa");
        var tasaOCuota = 0.16m;
        var importe = CreateImporte();

        var traslado = new TrasladoConcepto(@base, impuesto, tipoFactor, tasaOCuota, importe);

        Assert.Equal(@base, traslado.Base);
        Assert.Equal(impuesto, traslado.Impuesto);
        Assert.Equal(tipoFactor, traslado.TipoFactor);
        Assert.Equal(tasaOCuota, traslado.TasaOCuota);
        Assert.Equal(importe, traslado.Importe);
        Assert.NotEqual(Guid.Empty, traslado.Id);
    }

    // --- Valid Construction: Cuota ---

    [Fact]
    public void Constructor_WithCuota_CreatesInstanceWithAllFields()
    {
        var @base = CreateBase();
        var impuesto = CreateImpuesto();
        var tipoFactor = CreateTipoFactor("Cuota");
        var tasaOCuota = 0.265500m;
        var importe = new MontoMoneda(26.55m, 2);

        var traslado = new TrasladoConcepto(@base, impuesto, tipoFactor, tasaOCuota, importe);

        Assert.Equal("Cuota", traslado.TipoFactor.Clave);
        Assert.Equal(tasaOCuota, traslado.TasaOCuota);
        Assert.Equal(importe, traslado.Importe);
    }

    // --- Valid Construction: Exento ---

    [Fact]
    public void Constructor_WithExento_CreatesInstanceWithNullTasaAndImporte()
    {
        var @base = CreateBase();
        var impuesto = CreateImpuesto();
        var tipoFactor = CreateTipoFactor("Exento");

        var traslado = new TrasladoConcepto(@base, impuesto, tipoFactor, null, null);

        Assert.Equal(@base, traslado.Base);
        Assert.Equal(impuesto, traslado.Impuesto);
        Assert.Equal(tipoFactor, traslado.TipoFactor);
        Assert.Null(traslado.TasaOCuota);
        Assert.Null(traslado.Importe);
    }

    // --- Null argument validations ---

    [Fact]
    public void Constructor_NullBase_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TrasladoConcepto(null!, CreateImpuesto(), CreateTipoFactor("Tasa"), 0.16m, CreateImporte()));
    }

    [Fact]
    public void Constructor_NullImpuesto_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TrasladoConcepto(CreateBase(), null!, CreateTipoFactor("Tasa"), 0.16m, CreateImporte()));
    }

    [Fact]
    public void Constructor_NullTipoFactor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TrasladoConcepto(CreateBase(), CreateImpuesto(), null!, 0.16m, CreateImporte()));
    }

    // --- Invalid TipoFactor value ---

    [Theory]
    [InlineData("Invalido")]
    [InlineData("tasa")]
    [InlineData("EXENTO")]
    [InlineData("Other")]
    public void Constructor_InvalidTipoFactor_ThrowsArgumentException(string clave)
    {
        var tipoFactor = CreateTipoFactor(clave);

        var ex = Assert.Throws<ArgumentException>(() =>
            new TrasladoConcepto(CreateBase(), CreateImpuesto(), tipoFactor, 0.16m, CreateImporte()));

        Assert.Equal("tipoFactor", ex.ParamName);
    }

    // --- Exento with non-null TasaOCuota ---

    [Fact]
    public void Constructor_ExentoWithTasaOCuota_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new TrasladoConcepto(CreateBase(), CreateImpuesto(), CreateTipoFactor("Exento"), 0.16m, null));

        Assert.Equal("tasaOCuota", ex.ParamName);
    }

    // --- Exento with non-null Importe ---

    [Fact]
    public void Constructor_ExentoWithImporte_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new TrasladoConcepto(CreateBase(), CreateImpuesto(), CreateTipoFactor("Exento"), null, CreateImporte()));

        Assert.Equal("importe", ex.ParamName);
    }

    // --- Tasa/Cuota with null TasaOCuota ---

    [Theory]
    [InlineData("Tasa")]
    [InlineData("Cuota")]
    public void Constructor_TasaOrCuotaWithNullTasaOCuota_ThrowsArgumentException(string clave)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new TrasladoConcepto(CreateBase(), CreateImpuesto(), CreateTipoFactor(clave), null, CreateImporte()));

        Assert.Equal("tasaOCuota", ex.ParamName);
    }

    // --- Tasa/Cuota with null Importe ---

    [Theory]
    [InlineData("Tasa")]
    [InlineData("Cuota")]
    public void Constructor_TasaOrCuotaWithNullImporte_ThrowsArgumentException(string clave)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new TrasladoConcepto(CreateBase(), CreateImpuesto(), CreateTipoFactor(clave), 0.16m, null));

        Assert.Equal("importe", ex.ParamName);
    }
}

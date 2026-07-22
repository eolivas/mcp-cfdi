using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class TrasladoGlobalTests
{
    private static MontoMoneda MakeMonto(decimal valor = 100m) => new(valor, 2);
    private static ClaveCatalogo MakeImpuesto() => new("c_Impuesto", "002");
    private static ClaveCatalogo MakeTipoFactor(string clave = "Tasa") => new("c_TipoFactor", clave);

    [Fact]
    public void Constructor_WithValidValues_CreatesInstance()
    {
        var @base = MakeMonto(1000m);
        var impuesto = MakeImpuesto();
        var tipoFactor = MakeTipoFactor();

        var traslado = new TrasladoGlobal(@base, impuesto, tipoFactor, 0.16m, MakeMonto(160m));

        Assert.Equal(@base, traslado.Base);
        Assert.Equal(impuesto, traslado.Impuesto);
        Assert.Equal(tipoFactor, traslado.TipoFactor);
        Assert.Equal(0.16m, traslado.TasaOCuota);
        Assert.NotNull(traslado.Importe);
        Assert.Equal(160m, traslado.Importe!.Valor);
    }

    [Fact]
    public void Constructor_TipoFactorExento_WithNullTasaAndImporte_CreatesInstance()
    {
        var @base = MakeMonto(500m);
        var impuesto = MakeImpuesto();
        var tipoFactor = MakeTipoFactor("Exento");

        var traslado = new TrasladoGlobal(@base, impuesto, tipoFactor, null, null);

        Assert.Equal(@base, traslado.Base);
        Assert.Null(traslado.TasaOCuota);
        Assert.Null(traslado.Importe);
    }

    [Fact]
    public void Constructor_TipoFactorExento_WithTasaOCuota_ThrowsArgumentException()
    {
        var @base = MakeMonto();
        var impuesto = MakeImpuesto();
        var tipoFactor = MakeTipoFactor("Exento");

        var ex = Assert.Throws<ArgumentException>(
            () => new TrasladoGlobal(@base, impuesto, tipoFactor, 0.16m, null));

        Assert.Contains("TasaOCuota", ex.Message);
    }

    [Fact]
    public void Constructor_TipoFactorExento_WithImporte_ThrowsArgumentException()
    {
        var @base = MakeMonto();
        var impuesto = MakeImpuesto();
        var tipoFactor = MakeTipoFactor("Exento");

        var ex = Assert.Throws<ArgumentException>(
            () => new TrasladoGlobal(@base, impuesto, tipoFactor, null, MakeMonto(50m)));

        Assert.Contains("Importe", ex.Message);
    }

    [Fact]
    public void Constructor_NullBase_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TrasladoGlobal(null!, MakeImpuesto(), MakeTipoFactor(), 0.16m, MakeMonto()));
    }

    [Fact]
    public void Constructor_NullImpuesto_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TrasladoGlobal(MakeMonto(), null!, MakeTipoFactor(), 0.16m, MakeMonto()));
    }

    [Fact]
    public void Constructor_NullTipoFactor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TrasladoGlobal(MakeMonto(), MakeImpuesto(), null!, 0.16m, MakeMonto()));
    }
}

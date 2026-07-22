using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class ImpuestosGlobalTests
{
    private static MontoMoneda MakeMonto(decimal valor = 100m) => new(valor, 2);
    private static ClaveCatalogo MakeImpuesto() => new("c_Impuesto", "002");
    private static ClaveCatalogo MakeTipoFactor() => new("c_TipoFactor", "Tasa");

    private static TrasladoGlobal MakeTraslado(decimal baseVal = 1000m, decimal importe = 160m)
        => new(MakeMonto(baseVal), MakeImpuesto(), MakeTipoFactor(), 0.16m, MakeMonto(importe));

    [Fact]
    public void Constructor_WithValidValues_CreatesInstance()
    {
        var total = MakeMonto(160m);
        var traslados = new List<TrasladoGlobal> { MakeTraslado() };

        var impuestos = new ImpuestosGlobal(total, traslados);

        Assert.Equal(total, impuestos.TotalImpuestosTrasladados);
        Assert.Single(impuestos.Traslados);
    }

    [Fact]
    public void Constructor_WithMultipleTraslados_CreatesInstance()
    {
        var total = MakeMonto(320m);
        var traslados = new List<TrasladoGlobal>
        {
            MakeTraslado(1000m, 160m),
            MakeTraslado(2000m, 160m)
        };

        var impuestos = new ImpuestosGlobal(total, traslados);

        Assert.Equal(2, impuestos.Traslados.Count);
    }

    [Fact]
    public void Constructor_NullTotal_ThrowsArgumentNullException()
    {
        var traslados = new List<TrasladoGlobal> { MakeTraslado() };

        Assert.Throws<ArgumentNullException>(
            () => new ImpuestosGlobal(null!, traslados));
    }

    [Fact]
    public void Constructor_NullTraslados_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ImpuestosGlobal(MakeMonto(), null!));
    }

    [Fact]
    public void Constructor_EmptyTraslados_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new ImpuestosGlobal(MakeMonto(), new List<TrasladoGlobal>()));

        Assert.Contains("at least one item", ex.Message);
    }
}

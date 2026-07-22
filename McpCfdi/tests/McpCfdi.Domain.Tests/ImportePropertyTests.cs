using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

/// <summary>
/// Property 3: Importe de concepto = Cantidad × ValorUnitario redondeado
/// **Validates: Requirements 4.5, 5.5**
///
/// Para cualquier par de valores Cantidad (> 0, máx 6 decimales) y ValorUnitario (≥ 0)
/// y para cualquier moneda con N decimales definidos, el Importe calculado DEBERÁ ser igual a
/// Math.Round(Cantidad * ValorUnitario, N, MidpointRounding.AwayFromZero).
/// La misma fórmula aplica a nivel de traslado:
/// Importe del traslado = Math.Round(Base * TasaOCuota, N, MidpointRounding.AwayFromZero).
/// </summary>
public class ImportePropertyTests
{
    /// <summary>
    /// Generator for Cantidad: decimal > 0, max 6 decimal places.
    /// Produces values like 1.123456, 99.5, 0.000001, etc.
    /// </summary>
    private static Gen<decimal> GenCantidad()
    {
        return from intPart in Gen.Choose(1, 999999)
               from decPart in Gen.Choose(0, 999999)
               from decPlaces in Gen.Choose(0, 6)
               let raw = intPart + (decimal)decPart / 1_000_000m
               select Math.Round(raw, decPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Generator for currency decimal places: int between 0 and 6.
    /// Covers all SAT currency decimal configurations.
    /// </summary>
    private static Gen<int> GenDecimalesMoneda()
    {
        return Gen.Choose(0, 6);
    }

    /// <summary>
    /// Generator for ValorUnitario: non-negative decimal with reasonable range.
    /// </summary>
    private static Gen<decimal> GenValorUnitario()
    {
        return from intPart in Gen.Choose(0, 999999)
               from decPart in Gen.Choose(0, 999999)
               let raw = intPart + (decimal)decPart / 1_000_000m
               select raw;
    }

    /// <summary>
    /// Generator for TasaOCuota: decimal for tax rates.
    /// Common SAT values: 0.160000 (IVA 16%), 0.080000 (IVA 8%), 0.000000 (tasa 0).
    /// Also includes arbitrary rates between 0 and 1 for broader coverage.
    /// </summary>
    private static Gen<decimal> GenTasaOCuota()
    {
        var commonRates = Gen.Elements(0.160000m, 0.080000m, 0.000000m, 0.040000m);
        var arbitraryRates = from numerator in Gen.Choose(0, 1_000_000)
                             select (decimal)numerator / 1_000_000m;
        return Gen.Frequency(
            (3, commonRates),
            (7, arbitraryRates));
    }

    /// <summary>
    /// **Validates: Requirements 4.5, 5.5**
    /// For any Cantidad > 0 and ValorUnitario >= 0, the Importe computed by MontoMoneda
    /// must equal Math.Round(Cantidad * ValorUnitario, N, MidpointRounding.AwayFromZero).
    /// </summary>
    [Fact]
    public void ConceptoImporte_EqualsRoundedProduct()
    {
        var gen = from cantidad in GenCantidad()
                  from valorUnitario in GenValorUnitario()
                  from decimales in GenDecimalesMoneda()
                  select (cantidad, valorUnitario, decimales);

        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, tuple =>
        {
            var (cantidad, valorUnitario, decimales) = tuple;

            var expected = Math.Round(cantidad * valorUnitario, decimales, MidpointRounding.AwayFromZero);
            var montoMoneda = new MontoMoneda(cantidad * valorUnitario, decimales);

            return (montoMoneda.Valor == expected).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// **Validates: Requirements 4.5, 5.5**
    /// For any Base >= 0 and TasaOCuota >= 0, the Importe of a traslado computed by MontoMoneda
    /// must equal Math.Round(Base * TasaOCuota, N, MidpointRounding.AwayFromZero).
    /// </summary>
    [Fact]
    public void TrasladoImporte_EqualsRoundedProduct()
    {
        var gen = from baseValue in GenValorUnitario()
                  from tasaOCuota in GenTasaOCuota()
                  from decimales in GenDecimalesMoneda()
                  select (baseValue, tasaOCuota, decimales);

        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, tuple =>
        {
            var (baseValue, tasaOCuota, decimales) = tuple;

            var expected = Math.Round(baseValue * tasaOCuota, decimales, MidpointRounding.AwayFromZero);
            var montoMoneda = new MontoMoneda(baseValue * tasaOCuota, decimales);

            return (montoMoneda.Valor == expected).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

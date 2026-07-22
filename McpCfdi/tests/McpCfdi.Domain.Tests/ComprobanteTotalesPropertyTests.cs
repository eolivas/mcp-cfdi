using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Domain;
using McpCfdi.Domain.Entities;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

/// <summary>
/// Property 5: Fórmula de totales del comprobante
/// **Validates: Requirements 7.1, 7.2, 7.3**
///
/// Para cualquier conjunto de conceptos con importes y descuentos, y para cualquier configuración
/// de impuestos, los totales del nodo Comprobante DEBERÁN cumplir:
/// SubTotal = Σ(Concepto.Importe);
/// Descuento = Σ(Concepto.Descuento) cuando existan;
/// Total = SubTotal − Descuento + TotalImpuestosTrasladados − TotalImpuestosRetenidos,
/// donde cada componente se redondea al número de decimales de la moneda con redondeo half-up.
/// </summary>
public class ComprobanteTotalesPropertyTests
{
    private const int DecimalesMoneda = 2;

    // Fixed valid entities for comprobante creation
    private static readonly Rfc EmisorRfc = new("EKU9003173C9");
    private static readonly Rfc ReceptorRfc = new("XAXX010101000");

    private static readonly Emisor EmisorFijo = new(
        EmisorRfc,
        "Empresa de Prueba SA de CV",
        new ClaveCatalogo("c_RegimenFiscal", "601"));

    private static readonly Receptor ReceptorFijo = new(
        ReceptorRfc,
        "Publico en General",
        new CodigoPostal("06600"),
        new ClaveCatalogo("c_RegimenFiscal", "616"),
        new ClaveCatalogo("c_UsoCFDI", "G03"));

    /// <summary>
    /// **Validates: Requirements 7.1, 7.2, 7.3**
    /// For any valid set of conceptos with random importes, descuentos, and traslados,
    /// after calling CalcularTotales the comprobante totals must satisfy:
    /// SubTotal == round(Σ Concepto.Importe, 2)
    /// Descuento == round(Σ Concepto.Descuento, 2) when any concepto has descuento
    /// Total == round(SubTotal - Descuento + TotalImpuestosTrasladados, 2)
    /// </summary>
    [Fact]
    public void Totales_CumplenFormula_ParaCualquierConjuntoDeConceptos()
    {
        var genComprobante = GenComprobante();
        var arb = genComprobante.ToArbitrary();

        var config = Config.Quick.WithMaxTest(100);

        var prop = Prop.ForAll(arb, comprobante =>
        {
            comprobante.CalcularTotales(DecimalesMoneda);

            // 7.1: SubTotal = sum of all Concepto.Importe rounded
            var expectedSubTotal = Math.Round(
                comprobante.Conceptos.Sum(c => c.Importe.Valor),
                DecimalesMoneda,
                MidpointRounding.AwayFromZero);

            var subTotalOk = comprobante.SubTotal.Valor == expectedSubTotal;

            // 7.2: Descuento = sum of Concepto.Descuento where not null, rounded
            var conceptosConDescuento = comprobante.Conceptos
                .Where(c => c.Descuento is not null)
                .ToList();

            bool descuentoOk;
            if (conceptosConDescuento.Count > 0)
            {
                var expectedDescuento = Math.Round(
                    conceptosConDescuento.Sum(c => c.Descuento!.Valor),
                    DecimalesMoneda,
                    MidpointRounding.AwayFromZero);
                descuentoOk = comprobante.Descuento is not null
                    && comprobante.Descuento.Valor == expectedDescuento;
            }
            else
            {
                descuentoOk = comprobante.Descuento is null;
            }

            // 7.3: Total = SubTotal - Descuento + TotalImpuestosTrasladados
            var expectedTotal = Math.Round(
                comprobante.SubTotal.Valor
                - (comprobante.Descuento?.Valor ?? 0m)
                + (comprobante.Impuestos?.TotalImpuestosTrasladados.Valor ?? 0m),
                DecimalesMoneda,
                MidpointRounding.AwayFromZero);

            var totalOk = comprobante.Total.Valor == expectedTotal;

            return (subTotalOk && descuentoOk && totalOk).ToProperty();
        });

        prop.Check(config);
    }

    // --- Custom Generators ---

    /// <summary>
    /// Generates a valid Comprobante with 1-5 conceptos, some with descuento,
    /// some with ObjetoImp="02" (taxes) and some with "01" (no taxes).
    /// </summary>
    private static Gen<Comprobante> GenComprobante()
    {
        return from numConceptos in Gen.Choose(1, 5)
               from conceptos in Gen.ArrayOf(GenConcepto(), numConceptos)
               select Comprobante.Crear(
                   DateTime.UtcNow,
                   new ClaveCatalogo("c_FormaPago", "01"),
                   new ClaveCatalogo("c_Moneda", "MXN"),
                   new ClaveCatalogo("c_TipoDeComprobante", "I"),
                   new ClaveCatalogo("c_MetodoPago", "PUE"),
                   new CodigoPostal("06600"),
                   new ClaveCatalogo("c_Exportacion", "01"),
                   EmisorFijo,
                   ReceptorFijo,
                   conceptos.ToList());
    }

    /// <summary>
    /// Generates a Concepto with random importe, optional descuento,
    /// and optionally with tax transfers (ObjetoImp "02") or without taxes ("01").
    /// </summary>
    private static Gen<Concepto> GenConcepto()
    {
        return from importeCentavos in Gen.Choose(100, 100000)
               from hasDescuento in Gen.Elements(true, false)
               from descuentoCentavos in Gen.Choose(1, importeCentavos / 2 + 1)
               from objetoImpClave in Gen.Elements("01", "02")
               from tasaOCuota in Gen.Elements(0.16m, 0.08m)
               select CrearConcepto(
                   importeCentavos / 100m,
                   hasDescuento ? descuentoCentavos / 100m : null,
                   objetoImpClave,
                   tasaOCuota);
    }

    private static Concepto CrearConcepto(
        decimal importe,
        decimal? descuento,
        string objetoImpClave,
        decimal tasaOCuota)
    {
        var importeMonto = new MontoMoneda(importe, DecimalesMoneda);
        var descuentoMonto = descuento.HasValue
            ? new MontoMoneda(descuento.Value, DecimalesMoneda)
            : null;

        // ValorUnitario = Importe (quantity = 1)
        var valorUnitario = new MontoMoneda(importe, DecimalesMoneda);

        var concepto = new Concepto(
            claveProdServ: new ClaveCatalogo("c_ClaveProdServ", "01010101"),
            cantidad: 1m,
            claveUnidad: new ClaveCatalogo("c_ClaveUnidad", "H87"),
            descripcion: "Producto de prueba",
            valorUnitario: valorUnitario,
            importe: importeMonto,
            objetoImp: new ClaveCatalogo("c_ObjetoImp", objetoImpClave),
            descuento: descuentoMonto);

        // Add traslado if ObjetoImp == "02"
        if (objetoImpClave == "02")
        {
            var baseGravable = importeMonto;
            var importeImpuesto = new MontoMoneda(
                Math.Round(baseGravable.Valor * tasaOCuota, DecimalesMoneda, MidpointRounding.AwayFromZero),
                DecimalesMoneda);

            var traslado = new TrasladoConcepto(
                @base: baseGravable,
                impuesto: new ClaveCatalogo("c_Impuesto", "002"),
                tipoFactor: new ClaveCatalogo("c_TipoFactor", "Tasa"),
                tasaOCuota: tasaOCuota,
                importe: importeImpuesto);

            concepto.AgregarTraslado(traslado);
        }

        return concepto;
    }
}

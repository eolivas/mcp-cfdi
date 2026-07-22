using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Domain;
using McpCfdi.Domain.Entities;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

/// <summary>
/// Property 4: Totales globales de impuestos = suma de importes a nivel concepto
/// **Validates: Requirements 6.5, 6.6, 6.7**
///
/// Para cualquier CFDI con M conceptos y cada concepto con K traslados, los totales del nodo global
/// cfdi:Impuestos DEBERÁN satisfacer:
/// (a) TotalImpuestosTrasladados = suma de todos los Importe de traslados a nivel concepto;
/// (b) para cada combinación única de (Impuesto, TipoFactor, TasaOCuota), el Importe del traslado
///     global = suma de importes del grupo, y la Base del traslado global = suma de bases del grupo,
///     ambos redondeados a los decimales de la moneda.
/// </summary>
public class ImpuestosGlobalPropertyTests
{
    private const int Decimales = 2;

    /// <summary>
    /// **Validates: Requirements 6.5, 6.6, 6.7**
    /// Property 4(a): TotalImpuestosTrasladados equals the sum of all concepto traslado Importe values.
    /// Property 4(b): For each unique (Impuesto, TipoFactor, TasaOCuota) group, the global traslado
    /// Importe and Base equal the rounded sum of the respective group values.
    /// </summary>
    [Fact]
    public void GlobalTaxTotals_EqualSumOfConceptoTrasladoAmounts()
    {
        var gen = GenComprobante();
        var arb = gen.ToArbitrary();

        var config = Config.Quick.WithMaxTest(100);

        var prop = Prop.ForAll(arb, comprobante =>
        {
            comprobante.CalcularTotales(Decimales);

            // After CalcularTotales, Impuestos should not be null since all conceptos have ObjetoImp="02"
            var impuestos = comprobante.Impuestos;
            if (impuestos is null)
                return false.ToProperty();

            // Property (a): TotalImpuestosTrasladados == sum of all concepto traslado Importe values
            var allTrasladoImportes = comprobante.Conceptos
                .SelectMany(c => c.Traslados)
                .Sum(t => t.Importe!.Valor);

            var expectedTotal = Math.Round(allTrasladoImportes, Decimales, MidpointRounding.AwayFromZero);

            if (impuestos.TotalImpuestosTrasladados.Valor != expectedTotal)
                return false.ToProperty();

            // Property (b): For each group by (Impuesto.Clave, TipoFactor.Clave, TasaOCuota),
            // global traslado Importe == rounded sum of group importes
            // global traslado Base == rounded sum of group bases
            var groups = comprobante.Conceptos
                .SelectMany(c => c.Traslados)
                .GroupBy(t => (t.Impuesto.Clave, t.TipoFactor.Clave, t.TasaOCuota))
                .ToList();

            if (impuestos.Traslados.Count != groups.Count)
                return false.ToProperty();

            foreach (var group in groups)
            {
                var expectedImporte = Math.Round(
                    group.Sum(t => t.Importe!.Valor), Decimales, MidpointRounding.AwayFromZero);
                var expectedBase = Math.Round(
                    group.Sum(t => t.Base.Valor), Decimales, MidpointRounding.AwayFromZero);

                var globalTraslado = impuestos.Traslados.FirstOrDefault(t =>
                    t.Impuesto.Clave == group.Key.Item1 &&
                    t.TipoFactor.Clave == group.Key.Item2 &&
                    t.TasaOCuota == group.Key.TasaOCuota);

                if (globalTraslado is null)
                    return false.ToProperty();

                if (globalTraslado.Importe!.Valor != expectedImporte)
                    return false.ToProperty();

                if (globalTraslado.Base.Valor != expectedBase)
                    return false.ToProperty();
            }

            return true.ToProperty();
        });

        prop.Check(config);
    }

    // --- Custom Generators ---

    private static readonly decimal[] TasasValidas = [0.16m, 0.08m, 0.04m];
    private static readonly string[] ImpuestoClaves = ["002", "003"];

    /// <summary>
    /// Generates a valid Comprobante with 1-5 conceptos, each having 1-3 traslados (TipoFactor="Tasa").
    /// All conceptos have ObjetoImp="02" to ensure global Impuestos node is generated.
    /// </summary>
    private static Gen<Comprobante> GenComprobante()
    {
        return from numConceptos in Gen.Choose(1, 5)
               from conceptos in Gen.ArrayOf(GenConcepto(), numConceptos)
               select CrearComprobante(conceptos.ToList());
    }

    private static Comprobante CrearComprobante(List<Concepto> conceptos)
    {
        var emisorRfc = new Rfc("EKU9003173C9");
        var emisor = new Emisor(
            emisorRfc,
            "Empresa de Prueba SA de CV",
            new ClaveCatalogo("c_RegimenFiscal", "601"));

        var receptorRfc = new Rfc("XAXX010101000");
        var receptor = new Receptor(
            receptorRfc,
            "Publico en General",
            new CodigoPostal("06600"),
            new ClaveCatalogo("c_RegimenFiscal", "616"),
            new ClaveCatalogo("c_UsoCFDI", "S01"));

        return Comprobante.Crear(
            fecha: DateTime.Now,
            formaPago: new ClaveCatalogo("c_FormaPago", "01"),
            moneda: new ClaveCatalogo("c_Moneda", "MXN"),
            tipoDeComprobante: new ClaveCatalogo("c_TipoDeComprobante", "I"),
            metodoPago: new ClaveCatalogo("c_MetodoPago", "PUE"),
            lugarExpedicion: new CodigoPostal("06600"),
            exportacion: new ClaveCatalogo("c_Exportacion", "01"),
            emisor: emisor,
            receptor: receptor,
            conceptos: conceptos);
    }

    /// <summary>
    /// Generates a valid Concepto with ObjetoImp="02" and 1-3 traslados.
    /// </summary>
    private static Gen<Concepto> GenConcepto()
    {
        return from cantidad in Gen.Choose(1, 100)
               from valorUnitarioInt in Gen.Choose(1, 10000)
               from numTraslados in Gen.Choose(1, 3)
               from trasladoSpecs in Gen.ArrayOf(GenTrasladoSpec(), numTraslados)
               let valorUnitario = new MontoMoneda(valorUnitarioInt, Decimales)
               let importe = new MontoMoneda((decimal)cantidad * valorUnitarioInt, Decimales)
               let concepto = CrearConcepto(cantidad, valorUnitario, importe, trasladoSpecs.ToList())
               select concepto;
    }

    private static Concepto CrearConcepto(
        int cantidad,
        MontoMoneda valorUnitario,
        MontoMoneda importe,
        List<TrasladoSpec> trasladoSpecs)
    {
        var concepto = new Concepto(
            claveProdServ: new ClaveCatalogo("c_ClaveProdServ", "84111506"),
            cantidad: cantidad,
            claveUnidad: new ClaveCatalogo("c_ClaveUnidad", "E48"),
            descripcion: "Servicio de prueba",
            valorUnitario: valorUnitario,
            importe: importe,
            objetoImp: new ClaveCatalogo("c_ObjetoImp", "02"));

        foreach (var spec in trasladoSpecs)
        {
            var baseMonto = new MontoMoneda(importe.Valor, Decimales);
            var importeTraslado = new MontoMoneda(importe.Valor * spec.TasaOCuota, Decimales);

            var traslado = new TrasladoConcepto(
                @base: baseMonto,
                impuesto: new ClaveCatalogo("c_Impuesto", spec.ImpuestoClave),
                tipoFactor: new ClaveCatalogo("c_TipoFactor", "Tasa"),
                tasaOCuota: spec.TasaOCuota,
                importe: importeTraslado);

            concepto.AgregarTraslado(traslado);
        }

        return concepto;
    }

    /// <summary>
    /// Generates a traslado specification (Impuesto key + TasaOCuota rate).
    /// </summary>
    private static Gen<TrasladoSpec> GenTrasladoSpec()
    {
        return from impuesto in Gen.Elements(ImpuestoClaves)
               from tasa in Gen.Elements(TasasValidas)
               select new TrasladoSpec(impuesto, tasa);
    }

    private sealed record TrasladoSpec(string ImpuestoClave, decimal TasaOCuota);
}

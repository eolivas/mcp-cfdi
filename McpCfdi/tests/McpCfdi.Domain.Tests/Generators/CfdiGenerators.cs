using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Domain;
using McpCfdi.Domain.Entities;
using McpCfdi.Domain.ValueObjects;
using McpCfdi.Infrastructure.Persistence;

namespace McpCfdi.Domain.Tests.Generators;

/// <summary>
/// Custom FsCheck generators for CFDI domain types.
/// Used across Domain.Tests and Infrastructure.Tests for property-based testing.
/// </summary>
public static class CfdiGenerators
{
    // Characters valid for the letter positions of an RFC
    private static readonly char[] RfcLetters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ&Ñ".ToCharArray();

    // Characters valid for the homoclave (last 3 characters)
    private static readonly char[] RfcAlphanumeric =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    /// <summary>
    /// Generates valid 12-char (persona moral) or 13-char (persona física) RFCs
    /// matching SAT regex patterns.
    /// </summary>
    public static Gen<Rfc> ArbRfc()
    {
        var genMoral = GenRfcMoral();
        var genFisica = GenRfcFisica();

        return Gen.OneOf(genMoral, genFisica);
    }

    /// <summary>
    /// Generates a valid 12-character RFC for persona moral.
    /// Pattern: [A-ZÑ&]{3}\d{6}[A-Z0-9]{3}
    /// </summary>
    private static Gen<Rfc> GenRfcMoral()
    {
        return from c1 in Gen.Elements(RfcLetters)
               from c2 in Gen.Elements(RfcLetters)
               from c3 in Gen.Elements(RfcLetters)
               from d1 in Gen.Choose(0, 9)
               from d2 in Gen.Choose(0, 9)
               from d3 in Gen.Choose(0, 9)
               from d4 in Gen.Choose(0, 9)
               from d5 in Gen.Choose(0, 9)
               from d6 in Gen.Choose(0, 9)
               from h1 in Gen.Elements(RfcAlphanumeric)
               from h2 in Gen.Elements(RfcAlphanumeric)
               from h3 in Gen.Elements(RfcAlphanumeric)
               let valor = $"{c1}{c2}{c3}{d1}{d2}{d3}{d4}{d5}{d6}{h1}{h2}{h3}"
               select new Rfc(valor);
    }

    /// <summary>
    /// Generates a valid 13-character RFC for persona física.
    /// Pattern: [A-ZÑ&]{4}\d{6}[A-Z0-9]{3}
    /// </summary>
    private static Gen<Rfc> GenRfcFisica()
    {
        return from c1 in Gen.Elements(RfcLetters)
               from c2 in Gen.Elements(RfcLetters)
               from c3 in Gen.Elements(RfcLetters)
               from c4 in Gen.Elements(RfcLetters)
               from d1 in Gen.Choose(0, 9)
               from d2 in Gen.Choose(0, 9)
               from d3 in Gen.Choose(0, 9)
               from d4 in Gen.Choose(0, 9)
               from d5 in Gen.Choose(0, 9)
               from d6 in Gen.Choose(0, 9)
               from h1 in Gen.Elements(RfcAlphanumeric)
               from h2 in Gen.Elements(RfcAlphanumeric)
               from h3 in Gen.Elements(RfcAlphanumeric)
               let valor = $"{c1}{c2}{c3}{c4}{d1}{d2}{d3}{d4}{d5}{d6}{h1}{h2}{h3}"
               select new Rfc(valor);
    }

    /// <summary>
    /// Generates non-negative decimals with 0–6 decimal places.
    /// Range: [0, 999999.999999]
    /// </summary>
    public static Gen<MontoMoneda> ArbMontoMoneda()
    {
        return from intPart in Gen.Choose(0, 999999)
               from decPart in Gen.Choose(0, 999999)
               from decimales in Gen.Choose(0, 6)
               let raw = intPart + (decimal)decPart / 1_000_000m
               let rounded = Math.Round(raw, decimales, MidpointRounding.AwayFromZero)
               select new MontoMoneda(rounded, decimales);
    }

    /// <summary>
    /// Generates decimals strictly greater than 0, with at most 6 decimal places.
    /// Range: (0, 999999.999999]
    /// </summary>
    public static Gen<decimal> ArbCantidad()
    {
        return from intPart in Gen.Choose(1, 999999)
               from decPart in Gen.Choose(0, 999999)
               from decPlaces in Gen.Choose(0, 6)
               let raw = intPart + (decimal)decPart / 1_000_000m
               select Math.Round(raw, decPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Generates a valid Concepto with all fields within valid ranges.
    /// Importe is computed as round(Cantidad × ValorUnitario, decimalesMoneda).
    /// </summary>
    public static Gen<Concepto> ArbConcepto()
    {
        return ArbConcepto(2);
    }

    /// <summary>
    /// Generates a valid Concepto with all fields within valid ranges for a given decimalesMoneda.
    /// </summary>
    public static Gen<Concepto> ArbConcepto(int decimalesMoneda)
    {
        return from cantidad in ArbCantidad()
               from valorUnitarioInt in Gen.Choose(1, 99999)
               from valorUnitarioDec in Gen.Choose(0, 999999)
               from hasDescuento in Gen.Elements(true, false)
               from hasNoIdentificacion in Gen.Elements(true, false)
               from hasUnidad in Gen.Elements(true, false)
               from objetoImpClave in Gen.Elements("01", "02", "03")
               from descripcionLength in Gen.Choose(1, 50)
               from tasaOCuota in Gen.Elements(0.160000m, 0.080000m, 0.040000m)
               let valorUnitarioRaw = valorUnitarioInt + (decimal)valorUnitarioDec / 1_000_000m
               let valorUnitario = new MontoMoneda(valorUnitarioRaw, decimalesMoneda)
               let importe = new MontoMoneda(cantidad * valorUnitario.Valor, decimalesMoneda)
               let descuento = hasDescuento && importe.Valor > 0
                   ? new MontoMoneda(Math.Round(importe.Valor * 0.1m, decimalesMoneda, MidpointRounding.AwayFromZero), decimalesMoneda)
                   : null
               let descripcion = new string('A', descripcionLength)
               let noIdentificacion = hasNoIdentificacion ? "PROD-001" : null
               let unidad = hasUnidad ? "Pieza" : null
               let concepto = new Concepto(
                   claveProdServ: new ClaveCatalogo("c_ClaveProdServ", "01010101"),
                   cantidad: cantidad,
                   claveUnidad: new ClaveCatalogo("c_ClaveUnidad", "H87"),
                   descripcion: descripcion,
                   valorUnitario: valorUnitario,
                   importe: importe,
                   objetoImp: new ClaveCatalogo("c_ObjetoImp", objetoImpClave),
                   noIdentificacion: noIdentificacion,
                   unidad: unidad,
                   descuento: descuento)
               select AddTrasladoIfNeeded(concepto, objetoImpClave, importe, tasaOCuota, decimalesMoneda);
    }

    /// <summary>
    /// Generates a valid TrasladoConcepto with Base > 0 and valid TipoFactor.
    /// Supports all three TipoFactor values: "Tasa", "Cuota", "Exento".
    /// </summary>
    public static Gen<TrasladoConcepto> ArbTrasladoConcepto()
    {
        return ArbTrasladoConcepto(2);
    }

    /// <summary>
    /// Generates a valid TrasladoConcepto with Base > 0 and valid TipoFactor
    /// for a given decimalesMoneda.
    /// </summary>
    public static Gen<TrasladoConcepto> ArbTrasladoConcepto(int decimalesMoneda)
    {
        var genTasaCuota = from tipoFactor in Gen.Elements("Tasa", "Cuota")
                          from baseInt in Gen.Choose(1, 99999)
                          from baseDec in Gen.Choose(0, 999999)
                          from tasaOCuota in Gen.Elements(0.160000m, 0.080000m, 0.040000m, 0.000000m)
                          let baseRaw = baseInt + (decimal)baseDec / 1_000_000m
                          let baseMonto = new MontoMoneda(baseRaw, decimalesMoneda)
                          let importeVal = Math.Round(baseMonto.Valor * tasaOCuota, decimalesMoneda, MidpointRounding.AwayFromZero)
                          let importeMonto = new MontoMoneda(importeVal, decimalesMoneda)
                          select new TrasladoConcepto(
                              @base: baseMonto,
                              impuesto: new ClaveCatalogo("c_Impuesto", "002"),
                              tipoFactor: new ClaveCatalogo("c_TipoFactor", tipoFactor),
                              tasaOCuota: tasaOCuota,
                              importe: importeMonto);

        var genExento = from baseInt in Gen.Choose(1, 99999)
                        from baseDec in Gen.Choose(0, 999999)
                        let baseRaw = baseInt + (decimal)baseDec / 1_000_000m
                        let baseMonto = new MontoMoneda(baseRaw, decimalesMoneda)
                        select new TrasladoConcepto(
                            @base: baseMonto,
                            impuesto: new ClaveCatalogo("c_Impuesto", "002"),
                            tipoFactor: new ClaveCatalogo("c_TipoFactor", "Exento"),
                            tasaOCuota: null,
                            importe: null);

        return Gen.Frequency(
            (7, genTasaCuota),
            (3, genExento));
    }

    /// <summary>
    /// Generates an arithmetically consistent full Comprobante.
    /// Each Concepto's Importe = round(Cantidad × ValorUnitario, decimalesMoneda).
    /// Each TrasladoConcepto's Importe = round(Base × TasaOCuota, decimalesMoneda).
    /// After CalcularTotales(), SubTotal/Total/Impuestos are correct.
    /// AsignarSello is called with dummy values.
    /// </summary>
    public static Gen<Comprobante> ArbComprobante()
    {
        return ArbComprobante(2);
    }

    /// <summary>
    /// Generates an arithmetically consistent full Comprobante for a given decimalesMoneda.
    /// </summary>
    public static Gen<Comprobante> ArbComprobante(int decimalesMoneda)
    {
        return from numConceptos in Gen.Choose(1, 5)
               from conceptos in Gen.ArrayOf(ArbConcepto(decimalesMoneda), numConceptos)
               from emisorRfc in ArbRfc()
               from receptorRfc in ArbRfc()
               from nombreLength in Gen.Choose(1, 50)
               let emisor = new Emisor(
                   emisorRfc,
                   new string('E', nombreLength),
                   new ClaveCatalogo("c_RegimenFiscal", "601"))
               let receptor = new Receptor(
                   receptorRfc,
                   new string('R', nombreLength),
                   new CodigoPostal("06600"),
                   new ClaveCatalogo("c_RegimenFiscal", "616"),
                   new ClaveCatalogo("c_UsoCFDI", "G03"))
               let truncatedNow = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second)
               let comprobante = Comprobante.Crear(
                   truncatedNow,
                   new ClaveCatalogo("c_FormaPago", "01"),
                   new ClaveCatalogo("c_Moneda", "MXN"),
                   new ClaveCatalogo("c_TipoDeComprobante", "I"),
                   new ClaveCatalogo("c_MetodoPago", "PUE"),
                   new CodigoPostal("06600"),
                   new ClaveCatalogo("c_Exportacion", "01"),
                   emisor,
                   receptor,
                   conceptos.ToList())
               select FinalizeComprobante(comprobante, decimalesMoneda);
    }

    /// <summary>
    /// Generates a CatalogoEntry with optional vigencia dates.
    /// Produces entries that may have:
    /// - Both FechaInicioVigencia and FechaFinVigencia set
    /// - Only FechaInicioVigencia set
    /// - Only FechaFinVigencia set
    /// - Neither date set
    /// </summary>
    public static Gen<CatalogoEntry> ArbCatalogoEntry()
    {
        return from catalogo in Gen.Elements("c_RegimenFiscal", "c_FormaPago", "c_Moneda", "c_UsoCFDI", "c_ClaveProdServ")
               from clave in Gen.Choose(1, 999).Select(i => i.ToString("D3"))
               from descripcionLength in Gen.Choose(5, 50)
               from hasInicio in Gen.Elements(true, false)
               from hasFin in Gen.Elements(true, false)
               from inicioYear in Gen.Choose(2017, 2024)
               from inicioMonth in Gen.Choose(1, 12)
               from finYear in Gen.Choose(2024, 2030)
               from finMonth in Gen.Choose(1, 12)
               select new CatalogoEntry
               {
                   Id = 0,
                   NombreCatalogo = catalogo,
                   Clave = clave,
                   Descripcion = new string('D', descripcionLength),
                   FechaInicioVigencia = hasInicio
                       ? new DateTime(inicioYear, inicioMonth, 1)
                       : null,
                   FechaFinVigencia = hasFin
                       ? new DateTime(finYear, finMonth, 1)
                       : null,
                   Metadata = null
               };
    }

    // --- Helper Methods ---

    private static Concepto AddTrasladoIfNeeded(
        Concepto concepto,
        string objetoImpClave,
        MontoMoneda importe,
        decimal tasaOCuota,
        int decimalesMoneda)
    {
        if (objetoImpClave == "02")
        {
            var importeImpuesto = new MontoMoneda(
                Math.Round(importe.Valor * tasaOCuota, decimalesMoneda, MidpointRounding.AwayFromZero),
                decimalesMoneda);

            var traslado = new TrasladoConcepto(
                @base: importe,
                impuesto: new ClaveCatalogo("c_Impuesto", "002"),
                tipoFactor: new ClaveCatalogo("c_TipoFactor", "Tasa"),
                tasaOCuota: tasaOCuota,
                importe: importeImpuesto);

            concepto.AgregarTraslado(traslado);
        }

        return concepto;
    }

    private static Comprobante FinalizeComprobante(Comprobante comprobante, int decimalesMoneda)
    {
        comprobante.CalcularTotales(decimalesMoneda);
        comprobante.AsignarSello(
            sello: Convert.ToBase64String(new byte[32]),
            certificado: Convert.ToBase64String(new byte[64]),
            noCertificado: "12345678901234567890");
        return comprobante;
    }
}



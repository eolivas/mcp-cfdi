using McpCfdi.Domain;
using McpCfdi.Domain.Entities;
using McpCfdi.Domain.ValueObjects;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Helper methods for building minimal domain objects in infrastructure tests.
/// </summary>
internal static class TestHelper
{
    /// <summary>
    /// Creates a minimal Comprobante containing one Concepto with ObjetoImp="02"
    /// and the specified TrasladoConcepto attached. 
    /// CalcularTotales and AsignarSello are called to produce a fully serializable Comprobante.
    /// </summary>
    public static Comprobante CreateMinimalComprobanteWithTraslado(TrasladoConcepto traslado)
    {
        const int decimalesMoneda = 2;

        var valorUnitario = new MontoMoneda(100m, decimalesMoneda);
        var importe = new MontoMoneda(100m, decimalesMoneda);

        var concepto = new Concepto(
            claveProdServ: new ClaveCatalogo("c_ClaveProdServ", "01010101"),
            cantidad: 1m,
            claveUnidad: new ClaveCatalogo("c_ClaveUnidad", "H87"),
            descripcion: "Producto de prueba",
            valorUnitario: valorUnitario,
            importe: importe,
            objetoImp: new ClaveCatalogo("c_ObjetoImp", "02"));

        concepto.AgregarTraslado(traslado);

        var emisor = new Emisor(
            new Rfc("AAA010101AAA"),
            "Emisor Test",
            new ClaveCatalogo("c_RegimenFiscal", "601"));

        var receptor = new Receptor(
            new Rfc("XAXX010101000"),
            "Receptor Test",
            new CodigoPostal("06600"),
            new ClaveCatalogo("c_RegimenFiscal", "616"),
            new ClaveCatalogo("c_UsoCFDI", "G03"));

        var comprobante = Comprobante.Crear(
            DateTime.UtcNow,
            new ClaveCatalogo("c_FormaPago", "01"),
            new ClaveCatalogo("c_Moneda", "MXN"),
            new ClaveCatalogo("c_TipoDeComprobante", "I"),
            new ClaveCatalogo("c_MetodoPago", "PUE"),
            new CodigoPostal("06600"),
            new ClaveCatalogo("c_Exportacion", "01"),
            emisor,
            receptor,
            new List<Concepto> { concepto });

        comprobante.CalcularTotales(decimalesMoneda);
        comprobante.AsignarSello(
            sello: Convert.ToBase64String(new byte[32]),
            certificado: Convert.ToBase64String(new byte[64]),
            noCertificado: "12345678901234567890");

        return comprobante;
    }
}

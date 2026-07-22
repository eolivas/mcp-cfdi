using FsCheck;
using FsCheck.Fluent;
using System.Xml.Linq;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Infrastructure.Xml;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Property 2: XML serializado contiene todos los elementos obligatorios
/// **Validates: Requirements 1.1, 1.3, 2.1, 3.1, 4.2**
///
/// Para cualquier modelo de dominio Comprobante válido, la serialización DEBERÁ producir
/// un documento XML donde el nodo raíz cfdi:Comprobante con namespace http://www.sat.gob.mx/cfd/4
/// contenga todos los atributos obligatorios, y los nodos hijos cfdi:Emisor, cfdi:Receptor
/// y cfdi:Conceptos con sus atributos obligatorios.
/// </summary>
public class MandatoryElementsPropertyTests
{
    private static readonly XNamespace CfdiNs = "http://www.sat.gob.mx/cfd/4";

    /// <summary>
    /// **Validates: Requirements 1.1, 1.3, 2.1, 3.1, 4.2**
    /// For any valid Comprobante, the serialized XML MUST contain:
    /// 1. Root element cfdi:Comprobante with namespace http://www.sat.gob.mx/cfd/4
    /// 2. All mandatory root attributes
    /// 3. cfdi:Emisor with Rfc, Nombre, RegimenFiscal
    /// 4. cfdi:Receptor with Rfc, Nombre, DomicilioFiscalReceptor, RegimenFiscalReceptor, UsoCFDI
    /// 5. cfdi:Conceptos with at least one cfdi:Concepto with mandatory attributes
    /// </summary>
    [Fact]
    public void SerializedXml_ContainsAllMandatoryElements()
    {
        var arb = CfdiGenerators.ArbComprobante().ToArbitrary();
        var serializer = new CfdiXmlSerializer();

        var prop = Prop.ForAll(arb, comprobante =>
        {
            var doc = serializer.Serializar(comprobante);
            var root = doc.Root!;

            // 1. Root element is cfdi:Comprobante with correct namespace
            var rootIsCorrect = root.Name == CfdiNs + "Comprobante";

            // 2. All mandatory root attributes present
            var hasVersion = root.Attribute("Version") is not null;
            var hasFecha = root.Attribute("Fecha") is not null;
            var hasSello = root.Attribute("Sello") is not null;
            var hasFormaPago = root.Attribute("FormaPago") is not null;
            var hasNoCertificado = root.Attribute("NoCertificado") is not null;
            var hasCertificado = root.Attribute("Certificado") is not null;
            var hasSubTotal = root.Attribute("SubTotal") is not null;
            var hasMoneda = root.Attribute("Moneda") is not null;
            var hasTotal = root.Attribute("Total") is not null;
            var hasTipoDeComprobante = root.Attribute("TipoDeComprobante") is not null;
            var hasMetodoPago = root.Attribute("MetodoPago") is not null;
            var hasLugarExpedicion = root.Attribute("LugarExpedicion") is not null;
            var hasExportacion = root.Attribute("Exportacion") is not null;

            var rootAttributesPresent = hasVersion && hasFecha && hasSello && hasFormaPago
                && hasNoCertificado && hasCertificado && hasSubTotal && hasMoneda
                && hasTotal && hasTipoDeComprobante && hasMetodoPago
                && hasLugarExpedicion && hasExportacion;

            // 3. cfdi:Emisor node with mandatory attributes
            var emisor = root.Element(CfdiNs + "Emisor");
            var emisorPresent = emisor is not null;
            var emisorHasRfc = emisor?.Attribute("Rfc") is not null;
            var emisorHasNombre = emisor?.Attribute("Nombre") is not null;
            var emisorHasRegimenFiscal = emisor?.Attribute("RegimenFiscal") is not null;
            var emisorComplete = emisorPresent && emisorHasRfc && emisorHasNombre && emisorHasRegimenFiscal;

            // 4. cfdi:Receptor node with mandatory attributes
            var receptor = root.Element(CfdiNs + "Receptor");
            var receptorPresent = receptor is not null;
            var receptorHasRfc = receptor?.Attribute("Rfc") is not null;
            var receptorHasNombre = receptor?.Attribute("Nombre") is not null;
            var receptorHasDomicilio = receptor?.Attribute("DomicilioFiscalReceptor") is not null;
            var receptorHasRegimen = receptor?.Attribute("RegimenFiscalReceptor") is not null;
            var receptorHasUsoCfdi = receptor?.Attribute("UsoCFDI") is not null;
            var receptorComplete = receptorPresent && receptorHasRfc && receptorHasNombre
                && receptorHasDomicilio && receptorHasRegimen && receptorHasUsoCfdi;

            // 5. cfdi:Conceptos with at least one cfdi:Concepto with mandatory attributes
            var conceptos = root.Element(CfdiNs + "Conceptos");
            var conceptosPresent = conceptos is not null;
            var firstConcepto = conceptos?.Element(CfdiNs + "Concepto");
            var conceptoPresent = firstConcepto is not null;
            var conceptoHasClaveProdServ = firstConcepto?.Attribute("ClaveProdServ") is not null;
            var conceptoHasCantidad = firstConcepto?.Attribute("Cantidad") is not null;
            var conceptoHasClaveUnidad = firstConcepto?.Attribute("ClaveUnidad") is not null;
            var conceptoHasDescripcion = firstConcepto?.Attribute("Descripcion") is not null;
            var conceptoHasValorUnitario = firstConcepto?.Attribute("ValorUnitario") is not null;
            var conceptoHasImporte = firstConcepto?.Attribute("Importe") is not null;
            var conceptoHasObjetoImp = firstConcepto?.Attribute("ObjetoImp") is not null;
            var conceptoComplete = conceptosPresent && conceptoPresent
                && conceptoHasClaveProdServ && conceptoHasCantidad && conceptoHasClaveUnidad
                && conceptoHasDescripcion && conceptoHasValorUnitario && conceptoHasImporte
                && conceptoHasObjetoImp;

            var allPresent = rootIsCorrect && rootAttributesPresent
                && emisorComplete && receptorComplete && conceptoComplete;

            return allPresent.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

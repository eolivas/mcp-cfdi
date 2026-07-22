using System.Globalization;
using System.Xml.Linq;
using McpCfdi.Domain;
using McpCfdi.Domain.Entities;
using McpCfdi.Domain.Interfaces;
using McpCfdi.Domain.ValueObjects;
using McpCfdi.Infrastructure.Exceptions;

namespace McpCfdi.Infrastructure.Xml;

/// <summary>
/// Serializes a <see cref="Comprobante"/> aggregate to CFDI 4.0 XML
/// using System.Xml.Linq, conforming to the SAT Anexo 20 schema.
/// </summary>
public class CfdiXmlSerializer : ICfdiSerializer
{
    private static readonly XNamespace CfdiNs = "http://www.sat.gob.mx/cfd/4";
    private static readonly XNamespace XsiNs = "http://www.w3.org/2001/XMLSchema-instance";

    private const string SchemaLocation = "http://www.sat.gob.mx/cfd/4 http://www.sat.gob.mx/sitio_internet/cfd/4/cfdv40.xsd";
    private const string DateFormat = "yyyy-MM-ddTHH:mm:ss";

    public XDocument Serializar(Comprobante comprobante)
    {
        ArgumentNullException.ThrowIfNull(comprobante, nameof(comprobante));

        var root = new XElement(CfdiNs + "Comprobante",
            new XAttribute(XNamespace.Xmlns + "cfdi", CfdiNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", XsiNs.NamespaceName),
            new XAttribute(XsiNs + "schemaLocation", SchemaLocation));

        // Root attributes
        root.Add(new XAttribute("Version", "4.0"));
        root.Add(new XAttribute("Fecha", comprobante.Fecha.ToString(DateFormat, CultureInfo.InvariantCulture)));
        root.Add(new XAttribute("FormaPago", comprobante.FormaPago.Clave));

        if (!string.IsNullOrEmpty(comprobante.NoCertificado))
            root.Add(new XAttribute("NoCertificado", comprobante.NoCertificado));

        if (!string.IsNullOrEmpty(comprobante.Certificado))
            root.Add(new XAttribute("Certificado", comprobante.Certificado));

        root.Add(new XAttribute("SubTotal", comprobante.SubTotal.FormatearParaXml()));

        if (comprobante.Descuento is not null)
            root.Add(new XAttribute("Descuento", comprobante.Descuento.FormatearParaXml()));

        root.Add(new XAttribute("Moneda", comprobante.Moneda.Clave));
        root.Add(new XAttribute("Total", comprobante.Total.FormatearParaXml()));
        root.Add(new XAttribute("TipoDeComprobante", comprobante.TipoDeComprobante.Clave));
        root.Add(new XAttribute("MetodoPago", comprobante.MetodoPago.Clave));
        root.Add(new XAttribute("LugarExpedicion", comprobante.LugarExpedicion.Valor));
        root.Add(new XAttribute("Exportacion", comprobante.Exportacion.Clave));

        if (comprobante.Sello is not null)
            root.Add(new XAttribute("Sello", comprobante.Sello));

        // Child nodes in XSD order:
        // InformacionGlobal, CfdiRelacionados, Emisor, Receptor, Conceptos, Impuestos, Complemento, Addenda

        // Emisor
        root.Add(SerializarEmisor(comprobante.Emisor));

        // Receptor
        root.Add(SerializarReceptor(comprobante.Receptor));

        // Conceptos
        root.Add(SerializarConceptos(comprobante.Conceptos));

        // Impuestos (optional)
        if (comprobante.Impuestos is not null)
            root.Add(SerializarImpuestos(comprobante.Impuestos));

        var doc = new XDocument(root);
        doc.Declaration = null;

        return doc;
    }

    public Comprobante Deserializar(XDocument xml)
    {
        ArgumentNullException.ThrowIfNull(xml, nameof(xml));

        var root = xml.Root
            ?? throw new XmlParsingException("El documento XML no tiene elemento raíz.");

        // Validate namespace and local name
        if (root.Name.Namespace != CfdiNs)
            throw new XmlParsingException(
                $"Namespace inválido: se esperaba '{CfdiNs.NamespaceName}' pero se encontró '{root.Name.NamespaceName}'.");

        if (root.Name.LocalName != "Comprobante")
            throw new XmlParsingException(
                $"Elemento raíz inválido: se esperaba 'Comprobante' pero se encontró '{root.Name.LocalName}'.");

        // Parse mandatory root attributes
        var fechaStr = GetMandatoryAttribute(root, "Fecha");
        var formaPagoStr = GetMandatoryAttribute(root, "FormaPago");
        var subTotalStr = GetMandatoryAttribute(root, "SubTotal");
        var monedaStr = GetMandatoryAttribute(root, "Moneda");
        var totalStr = GetMandatoryAttribute(root, "Total");
        var tipoDeComprobanteStr = GetMandatoryAttribute(root, "TipoDeComprobante");
        var metodoPagoStr = GetMandatoryAttribute(root, "MetodoPago");
        var lugarExpedicionStr = GetMandatoryAttribute(root, "LugarExpedicion");
        var exportacionStr = GetMandatoryAttribute(root, "Exportacion");
        var noCertificadoStr = GetMandatoryAttribute(root, "NoCertificado");
        var certificadoStr = GetMandatoryAttribute(root, "Certificado");
        var selloStr = GetMandatoryAttribute(root, "Sello");

        // Optional root attributes
        var descuentoStr = root.Attribute("Descuento")?.Value;

        // Determine decimal places from SubTotal string
        var decimalesMoneda = ContarDecimales(subTotalStr);

        // Parse fecha
        var fecha = DateTime.ParseExact(fechaStr, DateFormat, CultureInfo.InvariantCulture);

        // Parse catalog values
        var formaPago = new ClaveCatalogo("c_FormaPago", formaPagoStr);
        var moneda = new ClaveCatalogo("c_Moneda", monedaStr);
        var tipoDeComprobante = new ClaveCatalogo("c_TipoDeComprobante", tipoDeComprobanteStr);
        var metodoPago = new ClaveCatalogo("c_MetodoPago", metodoPagoStr);
        var lugarExpedicion = new CodigoPostal(lugarExpedicionStr);
        var exportacion = new ClaveCatalogo("c_Exportacion", exportacionStr);

        // Parse Emisor
        var emisorElement = root.Element(CfdiNs + "Emisor")
            ?? throw new XmlParsingException("No se encontró el nodo Emisor.");
        var emisor = DeserializarEmisor(emisorElement);

        // Parse Receptor
        var receptorElement = root.Element(CfdiNs + "Receptor")
            ?? throw new XmlParsingException("No se encontró el nodo Receptor.");
        var receptor = DeserializarReceptor(receptorElement);

        // Parse Conceptos
        var conceptosElement = root.Element(CfdiNs + "Conceptos")
            ?? throw new XmlParsingException("No se encontró el nodo Conceptos.");
        var conceptos = DeserializarConceptos(conceptosElement, decimalesMoneda);

        // Create Comprobante via factory method
        var comprobante = Comprobante.Crear(
            fecha,
            formaPago,
            moneda,
            tipoDeComprobante,
            metodoPago,
            lugarExpedicion,
            exportacion,
            emisor,
            receptor,
            conceptos);

        // Calculate totals (will recompute SubTotal, Total, Impuestos from conceptos)
        comprobante.CalcularTotales(decimalesMoneda);

        // Assign sello data if present
        if (!string.IsNullOrEmpty(selloStr) && !string.IsNullOrEmpty(certificadoStr) && !string.IsNullOrEmpty(noCertificadoStr))
        {
            comprobante.AsignarSello(selloStr, certificadoStr, noCertificadoStr);
        }

        return comprobante;
    }

    private static string GetMandatoryAttribute(XElement element, string attributeName)
    {
        var attr = element.Attribute(attributeName);
        if (attr is null || string.IsNullOrWhiteSpace(attr.Value))
            throw new XmlParsingException(
                $"Atributo obligatorio '{attributeName}' no encontrado en el elemento '{element.Name.LocalName}'.");
        return attr.Value;
    }

    private static int ContarDecimales(string valorStr)
    {
        var dotIndex = valorStr.IndexOf('.');
        return dotIndex < 0 ? 0 : valorStr.Length - dotIndex - 1;
    }

    private static Emisor DeserializarEmisor(XElement element)
    {
        var rfcStr = GetMandatoryAttribute(element, "Rfc");
        var nombre = GetMandatoryAttribute(element, "Nombre");
        var regimenFiscalStr = GetMandatoryAttribute(element, "RegimenFiscal");

        var rfc = new Rfc(rfcStr);
        var regimenFiscal = new ClaveCatalogo("c_RegimenFiscal", regimenFiscalStr);

        return new Emisor(rfc, nombre, regimenFiscal);
    }

    private static Receptor DeserializarReceptor(XElement element)
    {
        var rfcStr = GetMandatoryAttribute(element, "Rfc");
        var nombre = GetMandatoryAttribute(element, "Nombre");
        var domicilioStr = GetMandatoryAttribute(element, "DomicilioFiscalReceptor");
        var regimenStr = GetMandatoryAttribute(element, "RegimenFiscalReceptor");
        var usoCfdiStr = GetMandatoryAttribute(element, "UsoCFDI");

        var rfc = new Rfc(rfcStr);
        var domicilio = new CodigoPostal(domicilioStr);
        var regimenFiscal = new ClaveCatalogo("c_RegimenFiscal", regimenStr);
        var usoCfdi = new ClaveCatalogo("c_UsoCFDI", usoCfdiStr);

        return new Receptor(rfc, nombre, domicilio, regimenFiscal, usoCfdi);
    }

    private static List<Concepto> DeserializarConceptos(XElement conceptosElement, int decimalesMoneda)
    {
        var conceptos = new List<Concepto>();

        foreach (var conceptoEl in conceptosElement.Elements(CfdiNs + "Concepto"))
        {
            conceptos.Add(DeserializarConcepto(conceptoEl, decimalesMoneda));
        }

        if (conceptos.Count == 0)
            throw new XmlParsingException("El nodo Conceptos debe contener al menos un Concepto.");

        return conceptos;
    }

    private static Concepto DeserializarConcepto(XElement element, int decimalesMoneda)
    {
        var claveProdServStr = GetMandatoryAttribute(element, "ClaveProdServ");
        var cantidadStr = GetMandatoryAttribute(element, "Cantidad");
        var claveUnidadStr = GetMandatoryAttribute(element, "ClaveUnidad");
        var descripcion = GetMandatoryAttribute(element, "Descripcion");
        var valorUnitarioStr = GetMandatoryAttribute(element, "ValorUnitario");
        var importeStr = GetMandatoryAttribute(element, "Importe");
        var objetoImpStr = GetMandatoryAttribute(element, "ObjetoImp");

        // Optional attributes
        var noIdentificacion = element.Attribute("NoIdentificacion")?.Value;
        var unidad = element.Attribute("Unidad")?.Value;
        var descuentoStr = element.Attribute("Descuento")?.Value;

        var claveProdServ = new ClaveCatalogo("c_ClaveProdServ", claveProdServStr);
        var cantidad = decimal.Parse(cantidadStr, CultureInfo.InvariantCulture);
        var claveUnidad = new ClaveCatalogo("c_ClaveUnidad", claveUnidadStr);
        var valorUnitario = new MontoMoneda(
            decimal.Parse(valorUnitarioStr, CultureInfo.InvariantCulture),
            ContarDecimales(valorUnitarioStr));
        var importe = new MontoMoneda(
            decimal.Parse(importeStr, CultureInfo.InvariantCulture),
            ContarDecimales(importeStr));
        var objetoImp = new ClaveCatalogo("c_ObjetoImp", objetoImpStr);

        MontoMoneda? descuento = null;
        if (descuentoStr is not null)
        {
            descuento = new MontoMoneda(
                decimal.Parse(descuentoStr, CultureInfo.InvariantCulture),
                ContarDecimales(descuentoStr));
        }

        var concepto = new Concepto(
            claveProdServ,
            cantidad,
            claveUnidad,
            descripcion,
            valorUnitario,
            importe,
            objetoImp,
            noIdentificacion,
            unidad,
            descuento);

        // Parse traslados
        var impuestosEl = element.Element(CfdiNs + "Impuestos");
        if (impuestosEl is not null)
        {
            var trasladosEl = impuestosEl.Element(CfdiNs + "Traslados");
            if (trasladosEl is not null)
            {
                foreach (var trasladoEl in trasladosEl.Elements(CfdiNs + "Traslado"))
                {
                    var traslado = DeserializarTrasladoConcepto(trasladoEl, decimalesMoneda);
                    concepto.AgregarTraslado(traslado);
                }
            }
        }

        return concepto;
    }

    private static TrasladoConcepto DeserializarTrasladoConcepto(XElement element, int decimalesMoneda)
    {
        var baseStr = GetMandatoryAttribute(element, "Base");
        var impuestoStr = GetMandatoryAttribute(element, "Impuesto");
        var tipoFactorStr = GetMandatoryAttribute(element, "TipoFactor");

        var tasaOCuotaStr = element.Attribute("TasaOCuota")?.Value;
        var importeStr = element.Attribute("Importe")?.Value;

        var baseMonto = new MontoMoneda(
            decimal.Parse(baseStr, CultureInfo.InvariantCulture),
            ContarDecimales(baseStr));
        var impuesto = new ClaveCatalogo("c_Impuesto", impuestoStr);
        var tipoFactor = new ClaveCatalogo("c_TipoFactor", tipoFactorStr);

        decimal? tasaOCuota = tasaOCuotaStr is not null
            ? decimal.Parse(tasaOCuotaStr, CultureInfo.InvariantCulture)
            : null;

        MontoMoneda? importe = importeStr is not null
            ? new MontoMoneda(
                decimal.Parse(importeStr, CultureInfo.InvariantCulture),
                ContarDecimales(importeStr))
            : null;

        return new TrasladoConcepto(baseMonto, impuesto, tipoFactor, tasaOCuota, importe);
    }

    public string SerializarAString(Comprobante comprobante)
    {
        var doc = Serializar(comprobante);
        // Output without XML declaration
        return doc.Root!.ToString(SaveOptions.None);
    }

    private static XElement SerializarEmisor(Emisor emisor)
    {
        return new XElement(CfdiNs + "Emisor",
            new XAttribute("Rfc", emisor.Rfc.Valor),
            new XAttribute("Nombre", emisor.Nombre),
            new XAttribute("RegimenFiscal", emisor.RegimenFiscal.Clave));
    }

    private static XElement SerializarReceptor(Receptor receptor)
    {
        return new XElement(CfdiNs + "Receptor",
            new XAttribute("Rfc", receptor.Rfc.Valor),
            new XAttribute("Nombre", receptor.Nombre),
            new XAttribute("DomicilioFiscalReceptor", receptor.DomicilioFiscalReceptor.Valor),
            new XAttribute("RegimenFiscalReceptor", receptor.RegimenFiscalReceptor.Clave),
            new XAttribute("UsoCFDI", receptor.UsoCfdi.Clave));
    }

    private static XElement SerializarConceptos(IReadOnlyList<Concepto> conceptos)
    {
        var conceptosElement = new XElement(CfdiNs + "Conceptos");

        foreach (var concepto in conceptos)
        {
            conceptosElement.Add(SerializarConcepto(concepto));
        }

        return conceptosElement;
    }

    private static XElement SerializarConcepto(Concepto concepto)
    {
        var element = new XElement(CfdiNs + "Concepto",
            new XAttribute("ClaveProdServ", concepto.ClaveProdServ.Clave),
            new XAttribute("Cantidad", FormatearCantidad(concepto.Cantidad)),
            new XAttribute("ClaveUnidad", concepto.ClaveUnidad.Clave));

        if (concepto.NoIdentificacion is not null)
            element.Add(new XAttribute("NoIdentificacion", concepto.NoIdentificacion));

        if (concepto.Unidad is not null)
            element.Add(new XAttribute("Unidad", concepto.Unidad));

        element.Add(new XAttribute("Descripcion", concepto.Descripcion));
        element.Add(new XAttribute("ValorUnitario", concepto.ValorUnitario.FormatearParaXml()));
        element.Add(new XAttribute("Importe", concepto.Importe.FormatearParaXml()));

        if (concepto.Descuento is not null)
            element.Add(new XAttribute("Descuento", concepto.Descuento.FormatearParaXml()));

        element.Add(new XAttribute("ObjetoImp", concepto.ObjetoImp.Clave));

        // Impuestos del concepto (traslados)
        if (concepto.Traslados.Count > 0)
        {
            var impuestosElement = new XElement(CfdiNs + "Impuestos");
            var trasladosElement = new XElement(CfdiNs + "Traslados");

            foreach (var traslado in concepto.Traslados)
            {
                trasladosElement.Add(SerializarTrasladoConcepto(traslado));
            }

            impuestosElement.Add(trasladosElement);
            element.Add(impuestosElement);
        }

        return element;
    }

    private static XElement SerializarTrasladoConcepto(TrasladoConcepto traslado)
    {
        var element = new XElement(CfdiNs + "Traslado",
            new XAttribute("Base", traslado.Base.FormatearParaXml()),
            new XAttribute("Impuesto", traslado.Impuesto.Clave),
            new XAttribute("TipoFactor", traslado.TipoFactor.Clave));

        if (traslado.TasaOCuota is not null)
            element.Add(new XAttribute("TasaOCuota", FormatearTasaOCuota(traslado.TasaOCuota.Value)));

        if (traslado.Importe is not null)
            element.Add(new XAttribute("Importe", traslado.Importe.FormatearParaXml()));

        return element;
    }

    private static XElement SerializarImpuestos(Domain.ValueObjects.ImpuestosGlobal impuestos)
    {
        var element = new XElement(CfdiNs + "Impuestos",
            new XAttribute("TotalImpuestosTrasladados", impuestos.TotalImpuestosTrasladados.FormatearParaXml()));

        var trasladosElement = new XElement(CfdiNs + "Traslados");

        foreach (var traslado in impuestos.Traslados)
        {
            var trasladoElement = new XElement(CfdiNs + "Traslado",
                new XAttribute("Base", traslado.Base.FormatearParaXml()),
                new XAttribute("Impuesto", traslado.Impuesto.Clave),
                new XAttribute("TipoFactor", traslado.TipoFactor.Clave));

            if (traslado.TasaOCuota is not null)
                trasladoElement.Add(new XAttribute("TasaOCuota", FormatearTasaOCuota(traslado.TasaOCuota.Value)));

            if (traslado.Importe is not null)
                trasladoElement.Add(new XAttribute("Importe", traslado.Importe.FormatearParaXml()));

            trasladosElement.Add(trasladoElement);
        }

        element.Add(trasladosElement);
        return element;
    }

    /// <summary>
    /// Formats TasaOCuota with exactly 6 decimal places.
    /// </summary>
    private static string FormatearTasaOCuota(decimal tasaOCuota)
    {
        return tasaOCuota.ToString("F6", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats Cantidad with up to 6 decimal places, removing trailing zeros.
    /// </summary>
    private static string FormatearCantidad(decimal cantidad)
    {
        // Format with up to 6 decimal places and remove trailing zeros
        var formatted = cantidad.ToString("0.######", CultureInfo.InvariantCulture);
        return formatted;
    }
}

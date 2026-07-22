using System.Xml.Linq;
using McpCfdi.Domain.Entities;

namespace McpCfdi.Domain.Interfaces;

/// <summary>
/// Port for serializing and deserializing a <see cref="Comprobante"/> to/from XML.
/// Implementations use <see cref="XDocument"/> with full control over namespaces and attribute ordering
/// as required by the Anexo 20.
/// </summary>
public interface ICfdiSerializer
{
    /// <summary>
    /// Serializes a <see cref="Comprobante"/> domain model into an <see cref="XDocument"/>
    /// conforming to the CFDI 4.0 XSD schema.
    /// </summary>
    /// <param name="comprobante">The fully constructed Comprobante aggregate.</param>
    /// <returns>An XDocument representing the CFDI XML.</returns>
    XDocument Serializar(Comprobante comprobante);

    /// <summary>
    /// Deserializes a CFDI 4.0 XML document into its corresponding domain model.
    /// </summary>
    /// <param name="xml">An XDocument containing a valid CFDI 4.0 XML.</param>
    /// <returns>A <see cref="Comprobante"/> domain model populated from the XML.</returns>
    Comprobante Deserializar(XDocument xml);

    /// <summary>
    /// Serializes a <see cref="Comprobante"/> domain model to an XML string (without XML declaration),
    /// encoded in UTF-8 format.
    /// </summary>
    /// <param name="comprobante">The fully constructed Comprobante aggregate.</param>
    /// <returns>A string containing the CFDI XML.</returns>
    string SerializarAString(Comprobante comprobante);
}

using System.Xml.Linq;

namespace McpCfdi.Domain.Interfaces;

/// <summary>
/// Port for generating the "cadena original" (original chain) of a CFDI
/// by applying the official SAT XSLT transformation.
/// </summary>
public interface ICadenaOriginalGenerator
{
    /// <summary>
    /// Generates the cadena original by applying the SAT XSLT transformation to the CFDI XML.
    /// The result is a pipe-delimited string starting and ending with "||".
    /// </summary>
    /// <param name="cfdiXml">The CFDI XML document to transform.</param>
    /// <returns>The cadena original string.</returns>
    string Generar(XDocument cfdiXml);
}

using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using McpCfdi.Domain.Interfaces;
using McpCfdi.Infrastructure.Exceptions;

namespace McpCfdi.Infrastructure.Xml;

/// <summary>
/// Generates the "cadena original" (original chain) of a CFDI by applying
/// the SAT XSLT transformation embedded as an assembly resource.
/// The result is a pipe-delimited string starting and ending with "||".
/// </summary>
public class XsltCadenaOriginalGenerator : ICadenaOriginalGenerator
{
    private static readonly Lazy<XslCompiledTransform> CachedTransform = new(LoadXslt);

    private const string XsltResourceName = "McpCfdi.Infrastructure.Xml.cadenaoriginal_4_0.xslt";

    /// <inheritdoc />
    public string Generar(XDocument cfdiXml)
    {
        ArgumentNullException.ThrowIfNull(cfdiXml, nameof(cfdiXml));

        try
        {
            var xslt = CachedTransform.Value;

            using var inputReader = cfdiXml.CreateReader();
            using var outputWriter = new StringWriter();

            xslt.Transform(inputReader, null, outputWriter);

            return outputWriter.ToString();
        }
        catch (XsltException ex)
        {
            throw new XsltTransformException(
                "Error al aplicar la transformación XSLT para generar la cadena original.", ex);
        }
        catch (XmlException ex)
        {
            throw new XsltTransformException(
                "Error al procesar el XML del CFDI para la transformación XSLT.", ex);
        }
    }

    private static XslCompiledTransform LoadXslt()
    {
        var assembly = typeof(XsltCadenaOriginalGenerator).Assembly;

        using var stream = assembly.GetManifestResourceStream(XsltResourceName)
            ?? throw new XsltTransformException(
                $"No se encontró el recurso XSLT embebido '{XsltResourceName}' en el ensamblado.");

        using var reader = XmlReader.Create(stream);

        var xslt = new XslCompiledTransform();
        xslt.Load(reader);

        return xslt;
    }
}

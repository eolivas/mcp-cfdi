using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Infrastructure.Xml;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Property 8: Formato de la cadena original
/// **Validates: Requirements 10.2, 10.3**
///
/// Para cualquier CFDI válido serializado a XML, la cadena original generada por la
/// transformación XSLT DEBERÁ:
/// (a) iniciar con los caracteres `||`
/// (b) terminar con los caracteres `||`
/// (c) no contener retornos de carro (CR, `\r`)
/// (d) no contener tabuladores (`\t`)
/// (e) no contener dos o más espacios consecutivos
/// </summary>
public class CadenaOriginalFormatPropertyTests
{
    [Fact]
    public void CadenaOriginal_StartsAndEndsWithDoublePipe_NoForbiddenWhitespace()
    {
        var arb = CfdiGenerators.ArbComprobante().ToArbitrary();
        var serializer = new CfdiXmlSerializer();
        var cadenaGenerator = new XsltCadenaOriginalGenerator();

        var prop = Prop.ForAll(arb, comprobante =>
        {
            var xml = serializer.Serializar(comprobante);
            var cadena = cadenaGenerator.Generar(xml);

            var startsWithDoublePipe = cadena.StartsWith("||");
            var endsWithDoublePipe = cadena.EndsWith("||");
            var noCR = !cadena.Contains('\r');
            var noTAB = !cadena.Contains('\t');
            var noConsecutiveSpaces = !cadena.Contains("  ");

            return (startsWithDoublePipe && endsWithDoublePipe && noCR && noTAB && noConsecutiveSpaces).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

using FsCheck;
using FsCheck.Fluent;
using System.Xml.Linq;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Infrastructure.Xml;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Property 13: ObjetoImp="02" implica existencia de nodo Impuestos
/// **Validates: Requirements 5.1, 6.1, 6.2**
///
/// Para cualquier CFDI generado:
/// - Si algún concepto tiene ObjetoImp="02", el XML DEBE contener cfdi:Traslado dentro
///   del concepto correspondiente (cfdi:Concepto/cfdi:Impuestos/cfdi:Traslados/cfdi:Traslado)
///   y DEBE existir el nodo cfdi:Impuestos a nivel raíz del comprobante.
/// - Si NINGÚN concepto tiene ObjetoImp="02", el nodo cfdi:Impuestos a nivel raíz
///   DEBE estar ausente.
/// </summary>
public class ObjetoImpPropertyTests
{
    private static readonly XNamespace CfdiNs = "http://www.sat.gob.mx/cfd/4";

    /// <summary>
    /// **Validates: Requirements 5.1, 6.1, 6.2**
    /// </summary>
    [Fact]
    public void ObjetoImp02_ImpliesImpuestosNodeExists()
    {
        var arb = CfdiGenerators.ArbComprobante().ToArbitrary();
        var serializer = new CfdiXmlSerializer();

        var prop = Prop.ForAll(arb, comprobante =>
        {
            var doc = serializer.Serializar(comprobante);
            var root = doc.Root!;

            var conceptos = root.Element(CfdiNs + "Conceptos")!.Elements(CfdiNs + "Concepto").ToList();

            var hasConceptoWith02 = conceptos.Any(c => c.Attribute("ObjetoImp")?.Value == "02");
            var rootHasImpuestos = root.Element(CfdiNs + "Impuestos") is not null;

            if (hasConceptoWith02)
            {
                // Root must have Impuestos node
                if (!rootHasImpuestos)
                    return false.ToProperty().Label("ObjetoImp=02 present but root cfdi:Impuestos is missing");

                // Each concepto with ObjetoImp="02" must have Impuestos/Traslados/Traslado
                foreach (var concepto in conceptos.Where(c => c.Attribute("ObjetoImp")?.Value == "02"))
                {
                    var conceptoImpuestos = concepto.Element(CfdiNs + "Impuestos");
                    if (conceptoImpuestos is null)
                        return false.ToProperty().Label("Concepto with ObjetoImp=02 missing cfdi:Impuestos child");

                    var traslados = conceptoImpuestos.Element(CfdiNs + "Traslados");
                    if (traslados is null)
                        return false.ToProperty().Label("Concepto with ObjetoImp=02 missing cfdi:Traslados child");

                    var trasladoNodes = traslados.Elements(CfdiNs + "Traslado");
                    if (!trasladoNodes.Any())
                        return false.ToProperty().Label("Concepto with ObjetoImp=02 has no cfdi:Traslado nodes");
                }
            }
            else
            {
                // Root must NOT have Impuestos node
                if (rootHasImpuestos)
                    return false.ToProperty().Label("No ObjetoImp=02 but root cfdi:Impuestos is present");
            }

            return true.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

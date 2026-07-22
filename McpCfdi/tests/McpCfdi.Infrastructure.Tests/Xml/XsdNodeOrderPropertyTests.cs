using FsCheck;
using FsCheck.Fluent;
using System.Xml.Linq;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Infrastructure.Xml;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Property 15: Orden de nodos hijo conforme al XSD
/// **Validates: Requirements 8.8**
///
/// For any CFDI serialized, the direct child nodes of cfdi:Comprobante MUST appear
/// in the XSD sequence order:
/// 1. cfdi:InformacionGlobal (optional)
/// 2. cfdi:CfdiRelacionados (optional)
/// 3. cfdi:Emisor (mandatory)
/// 4. cfdi:Receptor (mandatory)
/// 5. cfdi:Conceptos (mandatory)
/// 6. cfdi:Impuestos (optional)
/// 7. cfdi:Complemento (optional)
/// 8. cfdi:Addenda (optional)
/// </summary>
public class XsdNodeOrderPropertyTests
{
    private static readonly XNamespace CfdiNs = "http://www.sat.gob.mx/cfd/4";

    // The canonical XSD order for child nodes of cfdi:Comprobante
    private static readonly string[] XsdOrder =
    {
        "InformacionGlobal",
        "CfdiRelacionados",
        "Emisor",
        "Receptor",
        "Conceptos",
        "Impuestos",
        "Complemento",
        "Addenda"
    };

    /// <summary>
    /// **Validates: Requirements 8.8**
    /// For any generated Comprobante serialized to XML, the direct child elements
    /// in the cfdi namespace must appear in the correct XSD sequence order.
    /// </summary>
    [Fact]
    public void ChildNodes_AppearInXsdOrder()
    {
        var arb = CfdiGenerators.ArbComprobante().ToArbitrary();
        var serializer = new CfdiXmlSerializer();

        var prop = Prop.ForAll(arb, comprobante =>
        {
            var doc = serializer.Serializar(comprobante);
            var root = doc.Root!;

            // Get local names of direct child elements (in cfdi namespace)
            var childNames = root.Elements()
                .Where(e => e.Name.Namespace == CfdiNs)
                .Select(e => e.Name.LocalName)
                .ToList();

            // Get their indices in the XSD order
            var indices = childNames
                .Select(name => Array.IndexOf(XsdOrder, name))
                .Where(idx => idx >= 0)
                .ToList();

            // Verify indices are monotonically non-decreasing (i.e., in order)
            for (int i = 1; i < indices.Count; i++)
            {
                if (indices[i] < indices[i - 1])
                    return false.ToProperty()
                        .Label($"Node order violation: '{childNames[i]}' (XSD pos {indices[i]}) appeared after '{childNames[i - 1]}' (XSD pos {indices[i - 1]})");
            }

            return true.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

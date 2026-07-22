using FsCheck;
using FsCheck.Fluent;
using System.Xml.Linq;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Infrastructure.Xml;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Property 7: TipoFactor determina conjunto de atributos del traslado
/// **Validates: Requirements 5.2, 5.3, 6.4**
///
/// Para cualquier nodo cfdi:Traslado (tanto a nivel concepto como global):
/// - IF TipoFactor es "Tasa" o "Cuota" → el nodo DEBERÁ contener: Base, Impuesto, TipoFactor, TasaOCuota, Importe
/// - IF TipoFactor es "Exento" → el nodo DEBERÁ contener únicamente: Base, Impuesto, TipoFactor
///   (atributos TasaOCuota e Importe DEBERÁN estar ausentes)
/// </summary>
public class TipoFactorPropertyTests
{
    private static readonly XNamespace CfdiNs = "http://www.sat.gob.mx/cfd/4";
    private readonly CfdiXmlSerializer _serializer = new();

    /// <summary>
    /// **Validates: Requirements 5.2, 5.3, 6.4**
    /// Using ArbComprobante: for any generated Comprobante serialized to XML,
    /// all cfdi:Traslado nodes (concept-level and global) must have attributes
    /// consistent with their TipoFactor value.
    /// </summary>
    [Fact]
    public void TipoFactor_DeterminesAttributePresence_FullComprobante()
    {
        var gen = CfdiGenerators.ArbComprobante();
        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, comprobante =>
        {
            var doc = _serializer.Serializar(comprobante);

            // Find all cfdi:Traslado nodes in the entire document
            var trasladoNodes = doc.Descendants(CfdiNs + "Traslado").ToList();

            // Only verify if there are traslado nodes (ObjetoImp="02" conceptos generate them)
            foreach (var node in trasladoNodes)
            {
                var tipoFactor = node.Attribute("TipoFactor")?.Value;

                if (tipoFactor == "Tasa" || tipoFactor == "Cuota")
                {
                    // Must have Base, Impuesto, TipoFactor, TasaOCuota, Importe
                    if (node.Attribute("Base") is null)
                        return false.ToProperty().Label($"Tasa/Cuota traslado missing Base");
                    if (node.Attribute("Impuesto") is null)
                        return false.ToProperty().Label($"Tasa/Cuota traslado missing Impuesto");
                    if (node.Attribute("TipoFactor") is null)
                        return false.ToProperty().Label($"Tasa/Cuota traslado missing TipoFactor");
                    if (node.Attribute("TasaOCuota") is null)
                        return false.ToProperty().Label($"Tasa/Cuota traslado missing TasaOCuota");
                    if (node.Attribute("Importe") is null)
                        return false.ToProperty().Label($"Tasa/Cuota traslado missing Importe");
                }
                else if (tipoFactor == "Exento")
                {
                    // Must have Base, Impuesto, TipoFactor
                    if (node.Attribute("Base") is null)
                        return false.ToProperty().Label($"Exento traslado missing Base");
                    if (node.Attribute("Impuesto") is null)
                        return false.ToProperty().Label($"Exento traslado missing Impuesto");
                    if (node.Attribute("TipoFactor") is null)
                        return false.ToProperty().Label($"Exento traslado missing TipoFactor");
                    // TasaOCuota and Importe must be absent
                    if (node.Attribute("TasaOCuota") is not null)
                        return false.ToProperty().Label($"Exento traslado has TasaOCuota (should be absent)");
                    if (node.Attribute("Importe") is not null)
                        return false.ToProperty().Label($"Exento traslado has Importe (should be absent)");
                }
            }

            return true.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// **Validates: Requirements 5.2, 5.3, 6.4**
    /// Using ArbTrasladoConcepto directly: for any generated TrasladoConcepto,
    /// create a minimal Comprobante with that traslado, serialize to XML,
    /// and verify attributes match TipoFactor rules.
    /// </summary>
    [Fact]
    public void TipoFactor_DeterminesAttributePresence_DirectTraslado()
    {
        var gen = CfdiGenerators.ArbTrasladoConcepto();
        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, traslado =>
        {
            // Build a minimal Comprobante with one Concepto containing this traslado
            var comprobante = TestHelper.CreateMinimalComprobanteWithTraslado(traslado);
            var doc = _serializer.Serializar(comprobante);

            // Find traslado nodes within the concepto
            var trasladoNodes = doc.Descendants(CfdiNs + "Traslado").ToList();

            // Should have at least one from the concepto level
            if (trasladoNodes.Count == 0)
                return false.ToProperty().Label("No Traslado nodes found in serialized XML");

            foreach (var node in trasladoNodes)
            {
                var tipoFactorAttr = node.Attribute("TipoFactor")?.Value;

                if (tipoFactorAttr == "Tasa" || tipoFactorAttr == "Cuota")
                {
                    if (node.Attribute("Base") is null)
                        return false.ToProperty().Label("Tasa/Cuota missing Base");
                    if (node.Attribute("Impuesto") is null)
                        return false.ToProperty().Label("Tasa/Cuota missing Impuesto");
                    if (node.Attribute("TipoFactor") is null)
                        return false.ToProperty().Label("Tasa/Cuota missing TipoFactor");
                    if (node.Attribute("TasaOCuota") is null)
                        return false.ToProperty().Label("Tasa/Cuota missing TasaOCuota");
                    if (node.Attribute("Importe") is null)
                        return false.ToProperty().Label("Tasa/Cuota missing Importe");
                }
                else if (tipoFactorAttr == "Exento")
                {
                    if (node.Attribute("Base") is null)
                        return false.ToProperty().Label("Exento missing Base");
                    if (node.Attribute("Impuesto") is null)
                        return false.ToProperty().Label("Exento missing Impuesto");
                    if (node.Attribute("TipoFactor") is null)
                        return false.ToProperty().Label("Exento missing TipoFactor");
                    if (node.Attribute("TasaOCuota") is not null)
                        return false.ToProperty().Label("Exento has TasaOCuota (should be absent)");
                    if (node.Attribute("Importe") is not null)
                        return false.ToProperty().Label("Exento has Importe (should be absent)");
                }
            }

            return true.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

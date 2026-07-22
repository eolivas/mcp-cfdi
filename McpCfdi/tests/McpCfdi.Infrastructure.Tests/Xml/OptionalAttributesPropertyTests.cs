using FsCheck;
using FsCheck.Fluent;
using System.Xml.Linq;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Infrastructure.Xml;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Property 14: Atributos opcionales ausentes se omiten del XML
/// **Validates: Requirements 8.6**
///
/// For any Comprobante model where optional fields have null value,
/// the serialized XML MUST NOT contain attributes corresponding to those null fields.
/// </summary>
public class OptionalAttributesPropertyTests
{
    private static readonly XNamespace CfdiNs = "http://www.sat.gob.mx/cfd/4";
    private readonly CfdiXmlSerializer _serializer = new();

    /// <summary>
    /// **Validates: Requirements 8.6**
    /// Using ArbComprobante: for any generated Comprobante serialized to XML,
    /// null optional fields must not produce attributes in the output XML.
    /// </summary>
    [Fact]
    public void NullOptionalFields_AreAbsentFromXml()
    {
        var arb = CfdiGenerators.ArbComprobante().ToArbitrary();

        var prop = Prop.ForAll(arb, comprobante =>
        {
            var doc = _serializer.Serializar(comprobante);
            var root = doc.Root!;

            // Check Comprobante.Descuento: if null in model, attribute must be absent
            if (comprobante.Descuento is null)
            {
                if (root.Attribute("Descuento") is not null)
                    return false.ToProperty().Label("Comprobante.Descuento is null but 'Descuento' attribute present on root");
            }

            // Check each Concepto's optional fields
            var conceptoElements = root
                .Element(CfdiNs + "Conceptos")!
                .Elements(CfdiNs + "Concepto")
                .ToList();

            for (int i = 0; i < comprobante.Conceptos.Count; i++)
            {
                var concepto = comprobante.Conceptos[i];
                var conceptoEl = conceptoElements[i];

                // Concepto.NoIdentificacion
                if (concepto.NoIdentificacion is null)
                {
                    if (conceptoEl.Attribute("NoIdentificacion") is not null)
                        return false.ToProperty().Label($"Concepto[{i}].NoIdentificacion is null but attribute present");
                }

                // Concepto.Unidad
                if (concepto.Unidad is null)
                {
                    if (conceptoEl.Attribute("Unidad") is not null)
                        return false.ToProperty().Label($"Concepto[{i}].Unidad is null but attribute present");
                }

                // Concepto.Descuento
                if (concepto.Descuento is null)
                {
                    if (conceptoEl.Attribute("Descuento") is not null)
                        return false.ToProperty().Label($"Concepto[{i}].Descuento is null but attribute present");
                }

                // Check TrasladoConcepto optional fields (TasaOCuota, Importe — null when Exento)
                var trasladoNodes = conceptoEl
                    .Element(CfdiNs + "Impuestos")?
                    .Element(CfdiNs + "Traslados")?
                    .Elements(CfdiNs + "Traslado")
                    .ToList() ?? [];

                for (int j = 0; j < concepto.Traslados.Count; j++)
                {
                    var traslado = concepto.Traslados[j];
                    var trasladoEl = trasladoNodes[j];

                    // TrasladoConcepto.TasaOCuota
                    if (traslado.TasaOCuota is null)
                    {
                        if (trasladoEl.Attribute("TasaOCuota") is not null)
                            return false.ToProperty().Label($"Concepto[{i}].Traslado[{j}].TasaOCuota is null but attribute present");
                    }

                    // TrasladoConcepto.Importe
                    if (traslado.Importe is null)
                    {
                        if (trasladoEl.Attribute("Importe") is not null)
                            return false.ToProperty().Label($"Concepto[{i}].Traslado[{j}].Importe is null but attribute present");
                    }
                }
            }

            return true.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

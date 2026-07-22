using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Infrastructure.Xml;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Property 1: Round-trip de serialización/deserialización
/// **Validates: Requirements 9.1, 9.4, 9.5**
///
/// Para cualquier modelo de dominio Comprobante válido (con todos los campos obligatorios
/// poblados y valores dentro de rangos legales), serializarlo a XML y luego parsearlo de
/// vuelta DEBERÁ producir un modelo equivalente campo por campo al original, incluyendo
/// tanto atributos obligatorios como opcionales poblados.
/// </summary>
public class RoundTripPropertyTests
{
    [Fact]
    public void SerializeDeserialize_ProducesEquivalentModel()
    {
        var arb = CfdiGenerators.ArbComprobante().ToArbitrary();
        var serializer = new CfdiXmlSerializer();

        var prop = Prop.ForAll(arb, original =>
        {
            var xml = serializer.Serializar(original);
            var deserialized = serializer.Deserializar(xml);

            // Root-level fields
            var rootMatch =
                deserialized.Fecha == original.Fecha
                && deserialized.FormaPago.Clave == original.FormaPago.Clave
                && deserialized.NoCertificado == original.NoCertificado
                && deserialized.Certificado == original.Certificado
                && deserialized.SubTotal.Valor == original.SubTotal.Valor
                && deserialized.Moneda.Clave == original.Moneda.Clave
                && deserialized.Total.Valor == original.Total.Valor
                && deserialized.TipoDeComprobante.Clave == original.TipoDeComprobante.Clave
                && deserialized.MetodoPago.Clave == original.MetodoPago.Clave
                && deserialized.LugarExpedicion.Valor == original.LugarExpedicion.Valor
                && deserialized.Exportacion.Clave == original.Exportacion.Clave
                && deserialized.Sello == original.Sello
                && deserialized.Descuento?.Valor == original.Descuento?.Valor;

            // Emisor fields
            var emisorMatch =
                deserialized.Emisor.Rfc.Valor == original.Emisor.Rfc.Valor
                && deserialized.Emisor.Nombre == original.Emisor.Nombre
                && deserialized.Emisor.RegimenFiscal.Clave == original.Emisor.RegimenFiscal.Clave;

            // Receptor fields
            var receptorMatch =
                deserialized.Receptor.Rfc.Valor == original.Receptor.Rfc.Valor
                && deserialized.Receptor.Nombre == original.Receptor.Nombre
                && deserialized.Receptor.DomicilioFiscalReceptor.Valor == original.Receptor.DomicilioFiscalReceptor.Valor
                && deserialized.Receptor.RegimenFiscalReceptor.Clave == original.Receptor.RegimenFiscalReceptor.Clave
                && deserialized.Receptor.UsoCfdi.Clave == original.Receptor.UsoCfdi.Clave;

            // Conceptos count must match
            var conceptosCountMatch = deserialized.Conceptos.Count == original.Conceptos.Count;

            // Each Concepto field-by-field
            var conceptosMatch = conceptosCountMatch;
            if (conceptosCountMatch)
            {
                for (var i = 0; i < original.Conceptos.Count; i++)
                {
                    var orig = original.Conceptos[i];
                    var deser = deserialized.Conceptos[i];

                    conceptosMatch = conceptosMatch
                        && deser.ClaveProdServ.Clave == orig.ClaveProdServ.Clave
                        && deser.Cantidad == orig.Cantidad
                        && deser.ClaveUnidad.Clave == orig.ClaveUnidad.Clave
                        && deser.Descripcion == orig.Descripcion
                        && deser.ValorUnitario.Valor == orig.ValorUnitario.Valor
                        && deser.Importe.Valor == orig.Importe.Valor
                        && deser.ObjetoImp.Clave == orig.ObjetoImp.Clave
                        && deser.NoIdentificacion == orig.NoIdentificacion
                        && deser.Unidad == orig.Unidad
                        && deser.Descuento?.Valor == orig.Descuento?.Valor;

                    // Traslados within each Concepto
                    conceptosMatch = conceptosMatch
                        && deser.Traslados.Count == orig.Traslados.Count;

                    if (deser.Traslados.Count == orig.Traslados.Count)
                    {
                        for (var j = 0; j < orig.Traslados.Count; j++)
                        {
                            var origT = orig.Traslados[j];
                            var deserT = deser.Traslados[j];

                            conceptosMatch = conceptosMatch
                                && deserT.Base.Valor == origT.Base.Valor
                                && deserT.Impuesto.Clave == origT.Impuesto.Clave
                                && deserT.TipoFactor.Clave == origT.TipoFactor.Clave
                                && deserT.TasaOCuota == origT.TasaOCuota
                                && deserT.Importe?.Valor == origT.Importe?.Valor;
                        }
                    }
                }
            }

            // Impuestos global
            var impuestosMatch = true;
            if (original.Impuestos is null)
            {
                impuestosMatch = deserialized.Impuestos is null;
            }
            else
            {
                impuestosMatch = deserialized.Impuestos is not null
                    && deserialized.Impuestos.TotalImpuestosTrasladados.Valor == original.Impuestos.TotalImpuestosTrasladados.Valor
                    && deserialized.Impuestos.Traslados.Count == original.Impuestos.Traslados.Count;

                if (impuestosMatch && deserialized.Impuestos is not null)
                {
                    for (var i = 0; i < original.Impuestos.Traslados.Count; i++)
                    {
                        var origTG = original.Impuestos.Traslados[i];
                        var deserTG = deserialized.Impuestos.Traslados[i];

                        impuestosMatch = impuestosMatch
                            && deserTG.Base.Valor == origTG.Base.Valor
                            && deserTG.Impuesto.Clave == origTG.Impuesto.Clave
                            && deserTG.TipoFactor.Clave == origTG.TipoFactor.Clave
                            && deserTG.TasaOCuota == origTG.TasaOCuota
                            && deserTG.Importe?.Valor == origTG.Importe?.Valor;
                    }
                }
            }

            var allFieldsMatch = rootMatch && emisorMatch && receptorMatch && conceptosMatch && impuestosMatch;

            return allFieldsMatch.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

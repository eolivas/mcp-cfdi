using FsCheck;
using FsCheck.Fluent;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Infrastructure.Xml;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Property 16: Formato de fecha sin zona horaria
/// **Validates: Requirements 1.4**
///
/// Para cualquier DateTime asignado al atributo Fecha del comprobante,
/// la serialización DEBE producir una cadena que coincida exactamente con el patrón
/// yyyy-MM-ddTHH:mm:ss (sin indicador de zona horaria ni fracciones de segundo).
/// </summary>
public class FechaFormatPropertyTests
{
    private static readonly XNamespace CfdiNs = "http://www.sat.gob.mx/cfd/4";
    private static readonly Regex FechaRegex = new(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$");
    private readonly CfdiXmlSerializer _serializer = new();

    /// <summary>
    /// Generates DateTime values in a reasonable range (2000-2030)
    /// with various hour/minute/second combinations.
    /// </summary>
    private static Gen<DateTime> GenFecha()
    {
        return from year in Gen.Choose(2000, 2030)
               from month in Gen.Choose(1, 12)
               from day in Gen.Choose(1, 28) // Safe for all months
               from hour in Gen.Choose(0, 23)
               from minute in Gen.Choose(0, 59)
               from second in Gen.Choose(0, 59)
               select new DateTime(year, month, day, hour, minute, second);
    }

    /// <summary>
    /// **Validates: Requirements 1.4**
    /// For any generated Comprobante serialized to XML, the Fecha attribute
    /// must match the format yyyy-MM-ddTHH:mm:ss without timezone indicator
    /// or fractional seconds.
    /// </summary>
    [Fact]
    public void Fecha_FormattedWithoutTimezone()
    {
        var gen = CfdiGenerators.ArbComprobante();
        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, comprobante =>
        {
            var doc = _serializer.Serializar(comprobante);
            var root = doc.Root!;
            var fechaValue = root.Attribute("Fecha")?.Value;

            if (fechaValue is null)
                return false.ToProperty().Label("Fecha attribute is missing");

            // Must match exact pattern yyyy-MM-ddTHH:mm:ss
            if (!FechaRegex.IsMatch(fechaValue))
                return false.ToProperty().Label(
                    $"Fecha '{fechaValue}' does not match pattern yyyy-MM-ddTHH:mm:ss");

            // Must NOT contain timezone indicators
            if (fechaValue.Contains('Z'))
                return false.ToProperty().Label(
                    $"Fecha '{fechaValue}' contains 'Z' timezone indicator");

            if (fechaValue.Contains('+'))
                return false.ToProperty().Label(
                    $"Fecha '{fechaValue}' contains '+' timezone offset");

            // Must NOT contain fractional seconds
            if (fechaValue.Contains('.'))
                return false.ToProperty().Label(
                    $"Fecha '{fechaValue}' contains fractional seconds");

            return true.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

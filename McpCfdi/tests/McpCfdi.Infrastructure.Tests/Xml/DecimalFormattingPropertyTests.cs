using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Xml;

/// <summary>
/// Property 6: Formateo numérico respeta decimales de la moneda
/// **Validates: Requirements 7.6, 8.5**
///
/// Para cualquier valor numérico monetario serializado en el XML de un CFDI cuya moneda
/// define N decimales según el catálogo c_Moneda del SAT, la representación textual del valor
/// DEBERÁ contener exactamente N dígitos después del punto decimal, sin ceros a la izquierda
/// en la parte entera (excepto el caso de valor menor a 1).
/// </summary>
public class DecimalFormattingPropertyTests
{
    /// <summary>
    /// **Validates: Requirements 7.6, 8.5**
    /// For any MontoMoneda with N decimal places, FormatearParaXml() must produce
    /// a string with exactly N digits after the decimal point (or no decimal point if N=0),
    /// using '.' as decimal separator, and no leading zeros in the integer part
    /// (except "0.xx" for values less than 1).
    /// </summary>
    [Fact]
    public void MontoMoneda_FormatearParaXml_HasExactDecimalPlaces()
    {
        var arb = CfdiGenerators.ArbMontoMoneda().ToArbitrary();

        var prop = Prop.ForAll(arb, monto =>
        {
            var formatted = monto.FormatearParaXml();

            if (monto.Decimales == 0)
            {
                // No decimal point when 0 decimal places
                return (!formatted.Contains('.')).ToProperty();
            }
            else
            {
                var dotIndex = formatted.IndexOf('.');
                // Must contain a decimal point
                if (dotIndex < 0)
                    return false.ToProperty();

                // Exactly N digits after the decimal point
                var decimalDigits = formatted.Length - dotIndex - 1;
                return (decimalDigits == monto.Decimales).ToProperty();
            }
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// **Validates: Requirements 7.6, 8.5**
    /// For any MontoMoneda, FormatearParaXml() must use '.' as the decimal separator
    /// (InvariantCulture) and must not contain comma separators.
    /// </summary>
    [Fact]
    public void MontoMoneda_FormatearParaXml_UsesCorrectDecimalSeparator()
    {
        var arb = CfdiGenerators.ArbMontoMoneda().ToArbitrary();

        var prop = Prop.ForAll(arb, monto =>
        {
            var formatted = monto.FormatearParaXml();

            // Must not contain comma (thousands separator or alternate decimal separator)
            var noComma = !formatted.Contains(',');
            // If there's a decimal point, only one is allowed
            var singleDot = formatted.Count(c => c == '.') <= 1;

            return (noComma && singleDot).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// **Validates: Requirements 7.6, 8.5**
    /// For any MontoMoneda, FormatearParaXml() must not have leading zeros in the integer part,
    /// except for values less than 1 which start with "0.".
    /// </summary>
    [Fact]
    public void MontoMoneda_FormatearParaXml_NoLeadingZerosInIntegerPart()
    {
        var arb = CfdiGenerators.ArbMontoMoneda().ToArbitrary();

        var prop = Prop.ForAll(arb, monto =>
        {
            var formatted = monto.FormatearParaXml();

            var dotIndex = formatted.IndexOf('.');
            var integerPart = dotIndex >= 0 ? formatted[..dotIndex] : formatted;

            // Integer part must not have leading zeros unless value is 0
            if (integerPart.Length > 1)
                return (integerPart[0] != '0').ToProperty();

            // Single digit (including "0") is always valid
            return true.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

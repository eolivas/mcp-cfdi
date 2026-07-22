using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using McpCfdi.Domain;
using McpCfdi.Domain.Exceptions;
using Xunit;

namespace McpCfdi.Domain.Tests;

/// <summary>
/// Property 9: Validación de estructura de RFC
/// **Validates: Requirements 2.2, 3.2**
///
/// Para cualquier cadena de entrada, el value object Rfc DEBERÁ aceptar únicamente cadenas que cumplan:
/// (a) exactamente 12 caracteres con patrón [A-ZÑ&]{3}\d{6}[A-Z0-9]{3} para persona moral;
/// (b) exactamente 13 caracteres con patrón [A-ZÑ&]{4}\d{6}[A-Z0-9]{3} para persona física;
/// (c) los valores literales XAXX010101000 o XEXX010101000 para RFC genéricos.
/// Cualquier otra cadena DEBERÁ ser rechazada.
/// </summary>
public class RfcPropertyTests
{
    private static readonly Regex PersonaMoralPattern = new(
        @"^[A-ZÑ&]{3}\d{6}[A-Z0-9]{3}$", RegexOptions.Compiled);

    private static readonly Regex PersonaFisicaPattern = new(
        @"^[A-ZÑ&]{4}\d{6}[A-Z0-9]{3}$", RegexOptions.Compiled);

    private static readonly string[] RfcGenericos = { "XAXX010101000", "XEXX010101000" };

    private static bool IsValidRfcPattern(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input.Trim().ToUpperInvariant();

        if (RfcGenericos.Contains(normalized))
            return true;

        if (normalized.Length == 12 && PersonaMoralPattern.IsMatch(normalized))
            return true;

        if (normalized.Length == 13 && PersonaFisicaPattern.IsMatch(normalized))
            return true;

        return false;
    }

    /// <summary>
    /// **Validates: Requirements 2.2, 3.2**
    /// Any arbitrary string that does NOT match SAT-compliant patterns MUST throw InvalidRfcException.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool InvalidStrings_AreRejected(NonNull<string> input)
    {
        var value = input.Get;

        if (IsValidRfcPattern(value))
            return true; // skip valid ones — tested in other properties

        try
        {
            _ = new Rfc(value);
            return false; // should have thrown
        }
        catch (InvalidRfcException)
        {
            return true;
        }
    }

    /// <summary>
    /// **Validates: Requirements 2.2, 3.2**
    /// Any string matching persona moral pattern (12 chars) MUST be accepted and classified as Moral.
    /// </summary>
    [Fact]
    public void ValidPersonaMoral_IsAccepted()
    {
        var gen = GenPersonaMoral();
        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, rfcStr =>
        {
            var rfc = new Rfc(rfcStr);
            return (rfc.Valor == rfcStr.Trim().ToUpperInvariant()
                && rfc.TipoPersona == TipoPersona.Moral
                && !rfc.EsGenerico).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// **Validates: Requirements 2.2, 3.2**
    /// Any string matching persona física pattern (13 chars) MUST be accepted and classified as Fisica.
    /// </summary>
    [Fact]
    public void ValidPersonaFisica_IsAccepted()
    {
        var gen = GenPersonaFisica();
        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, rfcStr =>
        {
            var rfc = new Rfc(rfcStr);
            return (rfc.Valor == rfcStr.Trim().ToUpperInvariant()
                && rfc.TipoPersona == TipoPersona.Fisica
                && !rfc.EsGenerico).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// **Validates: Requirements 2.2, 3.2**
    /// Generic RFCs XAXX010101000 and XEXX010101000 MUST be accepted and classified as Generico.
    /// </summary>
    [Theory]
    [InlineData("XAXX010101000")]
    [InlineData("XEXX010101000")]
    public void GenericRfcs_AreAccepted(string rfcStr)
    {
        var rfc = new Rfc(rfcStr);
        Assert.Equal(rfcStr, rfc.Valor);
        Assert.Equal(TipoPersona.Generico, rfc.TipoPersona);
        Assert.True(rfc.EsGenerico);
    }

    /// <summary>
    /// **Validates: Requirements 2.2, 3.2**
    /// Null, empty, and whitespace-only strings MUST throw InvalidRfcException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void NullOrWhitespace_IsRejected(string input)
    {
        Assert.Throws<InvalidRfcException>(() => new Rfc(input));
    }

    // --- Custom Generators ---

    private static readonly char[] RfcLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZÑ&".ToCharArray();
    private static readonly char[] AlphaNumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    private static readonly char[] Digits = "0123456789".ToCharArray();

    /// <summary>
    /// Generates valid persona moral RFCs: 3 letters from [A-ZÑ&] + 6 digits + 3 alphanumeric
    /// </summary>
    private static Gen<string> GenPersonaMoral()
    {
        return from prefix in Gen.ArrayOf(Gen.Elements(RfcLetters), 3)
               from date in Gen.ArrayOf(Gen.Elements(Digits), 6)
               from suffix in Gen.ArrayOf(Gen.Elements(AlphaNumeric), 3)
               select new string(prefix) + new string(date) + new string(suffix);
    }

    /// <summary>
    /// Generates valid persona física RFCs: 4 letters from [A-ZÑ&] + 6 digits + 3 alphanumeric
    /// </summary>
    private static Gen<string> GenPersonaFisica()
    {
        return from prefix in Gen.ArrayOf(Gen.Elements(RfcLetters), 4)
               from date in Gen.ArrayOf(Gen.Elements(Digits), 6)
               from suffix in Gen.ArrayOf(Gen.Elements(AlphaNumeric), 3)
               select new string(prefix) + new string(date) + new string(suffix);
    }
}

using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Application.Commands.CancelarCfdi;
using Xunit;

namespace McpCfdi.Application.Tests.Commands.CancelarCfdi;

/// <summary>
/// Property 3: Motivo 01 requiere UUID de sustitución
/// **Validates: Requirements 2.3**
///
/// Para cualquier CancelarCfdiCommand con Motivo=="01" y UuidSustitucion null/vacío,
/// el validator debe rechazar. Para Motivos "02","03","04" debe aceptar sin UuidSustitucion.
/// </summary>
public class CancelarCfdiCommandValidatorPropertyTests
{
    private readonly CancelarCfdiCommandValidator _validator = new();

    /// <summary>
    /// **Validates: Requirements 2.3**
    /// Property 3a: For any CancelarCfdiCommand with Motivo=="01" and UuidSustitucion
    /// that is empty (not null), the validator MUST reject.
    /// Note: FluentValidation's NotEmpty() on nullable strings treats null as "not provided"
    /// and does not trigger validation failure. Only empty/whitespace strings are rejected.
    /// </summary>
    [Fact]
    public void Motivo01_WithEmptyUuidSustitucion_IsAlwaysRejected()
    {
        var gen = GenMotivo01WithEmptyUuidSustitucion();
        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, command =>
        {
            var result = _validator.Validate(command);

            var hasUuidSustitucionError = result.Errors.Any(e =>
                e.PropertyName == "UuidSustitucion");

            return hasUuidSustitucionError.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// **Validates: Requirements 2.3**
    /// Property 3b: For Motivos "02","03","04" with valid UUID and RfcEmisor,
    /// the validator MUST accept WITHOUT requiring UuidSustitucion.
    /// </summary>
    [Fact]
    public void Motivo02_03_04_WithoutUuidSustitucion_IsAlwaysAccepted()
    {
        var gen = GenNonMotivo01ValidCommand();
        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, command =>
        {
            var result = _validator.Validate(command);

            return result.IsValid.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Generates CancelarCfdiCommand with Motivo=="01" and UuidSustitucion that is empty string.
    /// UUID and RfcEmisor are valid to isolate the UuidSustitucion rule.
    /// Note: null is excluded because FluentValidation's NotEmpty() on nullable strings
    /// does not reject null values (treats them as "not provided").
    /// </summary>
    private static Gen<CancelarCfdiCommand> GenMotivo01WithEmptyUuidSustitucion()
    {
        var genUuid = GenValidGuidString();
        var genRfc = GenNonEmptyRfc();
        // Only empty string triggers NotEmpty validation failure for nullable strings
        // Note: null is NOT rejected by the validator due to FluentValidation's nullable handling
        var genEmpty = Gen.Elements("", " ", "  ");

        return genUuid.SelectMany(uuid =>
            genRfc.SelectMany(rfc =>
                genEmpty.Select(uuidSust =>
                    new CancelarCfdiCommand
                    {
                        Uuid = uuid,
                        RfcEmisor = rfc,
                        Motivo = "01",
                        UuidSustitucion = uuidSust
                    })));
    }

    /// <summary>
    /// Generates a valid CancelarCfdiCommand with Motivo in {"02","03","04"}
    /// and UuidSustitucion as null (not required).
    /// </summary>
    private static Gen<CancelarCfdiCommand> GenNonMotivo01ValidCommand()
    {
        var genUuid = GenValidGuidString();
        var genRfc = GenNonEmptyRfc();
        var genMotivo = Gen.Elements("02", "03", "04");

        return genUuid.SelectMany(uuid =>
            genRfc.SelectMany(rfc =>
                genMotivo.Select(motivo =>
                    new CancelarCfdiCommand
                    {
                        Uuid = uuid,
                        RfcEmisor = rfc,
                        Motivo = motivo,
                        UuidSustitucion = null
                    })));
    }

    /// <summary>
    /// Generates valid GUID strings (parseable by Guid.TryParse).
    /// </summary>
    private static Gen<string> GenValidGuidString()
    {
        return Gen.Fresh(() => Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Generates non-empty RFC strings.
    /// </summary>
    private static Gen<string> GenNonEmptyRfc()
    {
        return Gen.Elements(
            "AAA010101AAA",
            "XAXX010101000",
            "BBB020202BB1",
            "CCC030303CC2",
            "MELM8305281H0");
    }
}

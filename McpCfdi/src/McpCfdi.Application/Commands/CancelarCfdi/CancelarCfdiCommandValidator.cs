using FluentValidation;

namespace McpCfdi.Application.Commands.CancelarCfdi;

/// <summary>
/// FluentValidation validator for <see cref="CancelarCfdiCommand"/>.
/// Validates UUID format, RFC no vacío, motivo válido y UUID de sustitución condicional.
/// </summary>
public sealed class CancelarCfdiCommandValidator : AbstractValidator<CancelarCfdiCommand>
{
    private static readonly HashSet<string> MotivosValidos = ["01", "02", "03", "04"];

    public CancelarCfdiCommandValidator()
    {
        RuleFor(x => x.Uuid).NotEmpty().Must(BeValidUuid)
            .WithMessage("UUID debe ser un GUID válido.");

        RuleFor(x => x.RfcEmisor).NotEmpty();

        RuleFor(x => x.Motivo).NotEmpty()
            .Must(m => MotivosValidos.Contains(m))
            .WithMessage("Motivo debe ser 01, 02, 03 o 04.");

        RuleFor(x => x.UuidSustitucion)
            .NotEmpty().When(x => x.Motivo == "01")
            .WithMessage("UUID de sustitución es obligatorio para motivo 01.")
            .Must(BeValidUuid!).When(x => x.UuidSustitucion is not null)
            .WithMessage("UUID de sustitución debe ser un GUID válido.");
    }

    private static bool BeValidUuid(string? value) =>
        Guid.TryParse(value, out _);
}

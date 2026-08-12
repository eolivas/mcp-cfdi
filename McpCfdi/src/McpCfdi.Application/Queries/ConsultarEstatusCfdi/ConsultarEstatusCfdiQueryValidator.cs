using FluentValidation;

namespace McpCfdi.Application.Queries.ConsultarEstatusCfdi;

/// <summary>
/// Validador para <see cref="ConsultarEstatusCfdiQuery"/>.
/// </summary>
public class ConsultarEstatusCfdiQueryValidator : AbstractValidator<ConsultarEstatusCfdiQuery>
{
    public ConsultarEstatusCfdiQueryValidator()
    {
        RuleFor(x => x.RfcEmisor).NotEmpty().WithMessage("El RFC del emisor es requerido.");
        RuleFor(x => x.RfcReceptor).NotEmpty().WithMessage("El RFC del receptor es requerido.");
        RuleFor(x => x.Total).NotEmpty().WithMessage("El total es requerido.");
        RuleFor(x => x.Uuid)
            .NotEmpty().WithMessage("El UUID es requerido.")
            .Must(value => Guid.TryParse(value, out _))
            .WithMessage("El UUID debe tener formato GUID válido.");
    }
}

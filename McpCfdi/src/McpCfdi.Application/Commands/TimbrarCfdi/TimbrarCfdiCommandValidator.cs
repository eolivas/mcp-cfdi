using FluentValidation;

namespace McpCfdi.Application.Commands.TimbrarCfdi;

public class TimbrarCfdiCommandValidator : AbstractValidator<TimbrarCfdiCommand>
{
    public TimbrarCfdiCommandValidator()
    {
        RuleFor(x => x.CfdiXmlSellado)
            .NotEmpty().WithMessage("El XML del CFDI es requerido.")
            .Must(ContenerAtributoSello).WithMessage("El XML debe contener el atributo Sello.")
            .Must(ContenerAtributoNoCertificado).WithMessage("El XML debe contener el atributo NoCertificado.")
            .Must(ContenerAtributoCertificado).WithMessage("El XML debe contener el atributo Certificado.");
    }

    private static bool ContenerAtributoSello(string xml) =>
        xml.Contains("Sello=\"") && !xml.Contains("Sello=\"\"");

    private static bool ContenerAtributoNoCertificado(string xml) =>
        xml.Contains("NoCertificado=\"") && !xml.Contains("NoCertificado=\"\"");

    private static bool ContenerAtributoCertificado(string xml) =>
        xml.Contains("Certificado=\"") && !xml.Contains("Certificado=\"\"");
}

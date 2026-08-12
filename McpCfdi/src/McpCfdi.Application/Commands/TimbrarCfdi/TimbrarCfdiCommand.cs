using McpCfdi.Application.DTOs;
using MediatR;

namespace McpCfdi.Application.Commands.TimbrarCfdi;

/// <summary>
/// Comando para timbrar un CFDI 4.0 pre-sellado a través de un PAC.
/// </summary>
public record TimbrarCfdiCommand : IRequest<TimbradoResult>
{
    /// <summary>XML del CFDI 4.0 pre-sellado (con Sello, NoCertificado, Certificado).</summary>
    public required string CfdiXmlSellado { get; init; }
}

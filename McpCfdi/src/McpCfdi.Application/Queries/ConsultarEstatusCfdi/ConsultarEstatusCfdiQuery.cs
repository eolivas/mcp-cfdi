using McpCfdi.Application.DTOs;
using MediatR;

namespace McpCfdi.Application.Queries.ConsultarEstatusCfdi;

/// <summary>
/// Query para consultar el estatus de un CFDI ante el SAT.
/// </summary>
public record ConsultarEstatusCfdiQuery : IRequest<EstatusCfdiResult>
{
    /// <summary>RFC del emisor del comprobante.</summary>
    public required string RfcEmisor { get; init; }

    /// <summary>RFC del receptor del comprobante.</summary>
    public required string RfcReceptor { get; init; }

    /// <summary>Total del comprobante.</summary>
    public required string Total { get; init; }

    /// <summary>UUID (TimbreFiscalDigital) del comprobante.</summary>
    public required string Uuid { get; init; }
}

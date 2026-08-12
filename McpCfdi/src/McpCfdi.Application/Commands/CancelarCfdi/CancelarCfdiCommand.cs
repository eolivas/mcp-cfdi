using McpCfdi.Application.DTOs;
using MediatR;

namespace McpCfdi.Application.Commands.CancelarCfdi;

/// <summary>
/// Comando para cancelar un CFDI previamente timbrado ante el SAT.
/// </summary>
public record CancelarCfdiCommand : IRequest<CancelacionResult>
{
    /// <summary>UUID (folio fiscal) del CFDI a cancelar.</summary>
    public required string Uuid { get; init; }

    /// <summary>RFC del emisor del comprobante a cancelar.</summary>
    public required string RfcEmisor { get; init; }

    /// <summary>Motivo de cancelación según catálogo SAT: 01, 02, 03 o 04.</summary>
    public required string Motivo { get; init; }

    /// <summary>UUID del CFDI que sustituye al cancelado. Obligatorio cuando Motivo es "01".</summary>
    public string? UuidSustitucion { get; init; }
}

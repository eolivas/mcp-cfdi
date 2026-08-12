using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using MediatR;

namespace McpCfdi.Application.Queries.ConsultarEstatusCfdi;

public class ConsultarEstatusCfdiQueryHandler
    : IRequestHandler<ConsultarEstatusCfdiQuery, EstatusCfdiResult>
{
    private readonly IPacService _pacService;

    public ConsultarEstatusCfdiQueryHandler(IPacService pacService)
        => _pacService = pacService;

    public async Task<EstatusCfdiResult> Handle(
        ConsultarEstatusCfdiQuery request, CancellationToken ct)
    {
        var consultaRequest = new ConsultaEstatusRequest(
            RfcEmisor: request.RfcEmisor,
            RfcReceptor: request.RfcReceptor,
            Total: request.Total,
            Uuid: request.Uuid);

        return await _pacService.ConsultarEstatusAsync(consultaRequest, ct);
    }
}

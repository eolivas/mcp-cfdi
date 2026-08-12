using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using MediatR;

namespace McpCfdi.Application.Commands.TimbrarCfdi;

/// <summary>
/// Handler que delega el timbrado del CFDI al servicio PAC configurado.
/// </summary>
public class TimbrarCfdiCommandHandler : IRequestHandler<TimbrarCfdiCommand, TimbradoResult>
{
    private readonly IPacService _pacService;

    public TimbrarCfdiCommandHandler(IPacService pacService)
        => _pacService = pacService;

    public async Task<TimbradoResult> Handle(TimbrarCfdiCommand request, CancellationToken ct)
        => await _pacService.TimbrarAsync(request.CfdiXmlSellado, ct);
}

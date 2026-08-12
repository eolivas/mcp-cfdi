using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using MediatR;

namespace McpCfdi.Application.Commands.CancelarCfdi;

/// <summary>
/// Handler que orquesta la cancelación de un CFDI ante el PAC.
/// Carga credenciales CSD del emisor y delega la operación al servicio PAC.
/// </summary>
public class CancelarCfdiCommandHandler : IRequestHandler<CancelarCfdiCommand, CancelacionResult>
{
    private readonly IPacService _pacService;
    private readonly IEmisorCredencialesProvider _credencialesProvider;

    public CancelarCfdiCommandHandler(
        IPacService pacService,
        IEmisorCredencialesProvider credencialesProvider)
    {
        _pacService = pacService;
        _credencialesProvider = credencialesProvider;
    }

    public async Task<CancelacionResult> Handle(CancelarCfdiCommand request, CancellationToken ct)
    {
        var credenciales = await _credencialesProvider.ObtenerCredencialesAsync(request.RfcEmisor, ct);

        var cancelacionRequest = new CancelacionRequest(
            Uuid: request.Uuid,
            RfcEmisor: request.RfcEmisor,
            Motivo: request.Motivo,
            UuidSustitucion: request.UuidSustitucion,
            CertificadoBase64: Convert.ToBase64String(credenciales.CertificadoDer),
            LlavePrivadaBase64: Convert.ToBase64String(credenciales.LlavePrivadaDer),
            PasswordLlave: credenciales.PasswordLlave);

        return await _pacService.CancelarAsync(cancelacionRequest, ct);
    }
}

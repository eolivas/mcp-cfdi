using McpCfdi.Application.DTOs;

namespace McpCfdi.Application.Interfaces;

/// <summary>
/// Puerto que abstrae las operaciones con un Proveedor Autorizado de Certificación (PAC).
/// La implementación concreta se selecciona por configuración (Strategy pattern).
/// </summary>
public interface IPacService
{
    Task<TimbradoResult> TimbrarAsync(string cfdiXmlSellado, CancellationToken ct = default);
    Task<CancelacionResult> CancelarAsync(CancelacionRequest request, CancellationToken ct = default);
    Task<EstatusCfdiResult> ConsultarEstatusAsync(ConsultaEstatusRequest request, CancellationToken ct = default);
}

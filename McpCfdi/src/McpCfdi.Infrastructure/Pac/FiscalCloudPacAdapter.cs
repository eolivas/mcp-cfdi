using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;

namespace McpCfdi.Infrastructure.Pac;

// TODO: Full implementation in Task 7
/// <summary>
/// Adaptador PAC para el proveedor FiscalCloud.
/// </summary>
public class FiscalCloudPacAdapter : IPacService
{
    public Task<TimbradoResult> TimbrarAsync(string cfdiXmlSellado, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<CancelacionResult> CancelarAsync(CancelacionRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<EstatusCfdiResult> ConsultarEstatusAsync(ConsultaEstatusRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

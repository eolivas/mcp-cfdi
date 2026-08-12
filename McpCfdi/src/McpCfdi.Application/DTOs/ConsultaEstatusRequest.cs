namespace McpCfdi.Application.DTOs;

public record ConsultaEstatusRequest(
    string RfcEmisor,
    string RfcReceptor,
    string Total,
    string Uuid);

namespace McpCfdi.Application.DTOs;

public record CancelacionResult(
    string Uuid,
    string EstatusUuid,
    string AcuseXml,
    DateTime FechaCancelacion);

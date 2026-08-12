namespace McpCfdi.Application.DTOs;

public record EstatusCfdiResult(
    string Estado,
    string EstatusCancelacion,
    bool EsCancelable);

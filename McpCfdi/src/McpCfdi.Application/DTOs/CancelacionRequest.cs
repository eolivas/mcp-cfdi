namespace McpCfdi.Application.DTOs;

public record CancelacionRequest(
    string Uuid,
    string RfcEmisor,
    string Motivo,
    string? UuidSustitucion,
    string? CertificadoBase64,
    string? LlavePrivadaBase64,
    string? PasswordLlave);

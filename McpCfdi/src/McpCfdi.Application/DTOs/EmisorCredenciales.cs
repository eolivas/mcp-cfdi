namespace McpCfdi.Application.DTOs;

public record EmisorCredenciales(
    string Rfc,
    byte[] CertificadoDer,
    byte[] LlavePrivadaDer,
    string PasswordLlave);

using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using McpCfdi.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpCfdi.Infrastructure.Pac;

/// <summary>
/// Implementación que carga credenciales CSD desde el sistema de archivos.
/// Certificado y llave se leen de disco; el password se obtiene de variable de entorno.
/// </summary>
public class FileSystemEmisorCredencialesProvider : IEmisorCredencialesProvider
{
    private readonly EmisoresOptions _options;
    private readonly ILogger<FileSystemEmisorCredencialesProvider> _logger;

    public FileSystemEmisorCredencialesProvider(
        IOptions<EmisoresOptions> options,
        ILogger<FileSystemEmisorCredencialesProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmisorCredenciales> ObtenerCredencialesAsync(
        string rfc, CancellationToken ct = default)
    {
        var basePath = Path.Combine(_options.CertificadosDir, rfc);
        var cerPath = Path.Combine(basePath, "certificado.cer");
        var keyPath = Path.Combine(basePath, "llave.key");

        if (!File.Exists(cerPath))
            throw new EmisorCredencialesException(
                $"No se encontró el certificado para RFC {rfc} en: {cerPath}");

        if (!File.Exists(keyPath))
            throw new EmisorCredencialesException(
                $"No se encontró la llave privada para RFC {rfc} en: {keyPath}");

        var envKey = $"EMISOR__{rfc}__PASSWORD_LLAVE";
        var password = Environment.GetEnvironmentVariable(envKey);

        if (string.IsNullOrEmpty(password))
            throw new EmisorCredencialesException(
                $"No se encontró la variable de entorno '{envKey}' con el password de la llave privada.");

        var certificado = await File.ReadAllBytesAsync(cerPath, ct);
        var llavePrivada = await File.ReadAllBytesAsync(keyPath, ct);

        _logger.LogDebug("Credenciales CSD cargadas para emisor {Rfc}", rfc);

        return new EmisorCredenciales(rfc, certificado, llavePrivada, password);
    }

    public bool ExistenCredenciales(string rfc)
    {
        var basePath = Path.Combine(_options.CertificadosDir, rfc);
        return File.Exists(Path.Combine(basePath, "certificado.cer"))
            && File.Exists(Path.Combine(basePath, "llave.key"));
    }
}

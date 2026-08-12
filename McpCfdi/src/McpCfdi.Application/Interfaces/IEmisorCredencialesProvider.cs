using McpCfdi.Application.DTOs;

namespace McpCfdi.Application.Interfaces;

/// <summary>
/// Carga las credenciales CSD de un emisor desde disco (certificado y llave)
/// y la contraseña desde variable de entorno.
/// 
/// Estructura esperada en disco:
///   {CertificadosDir}/{RFC}/certificado.cer
///   {CertificadosDir}/{RFC}/llave.key
///
/// Variable de entorno para password:
///   EMISOR__{RFC}__PASSWORD_LLAVE  (ej: EMISOR__EKU9003173C9__PASSWORD_LLAVE)
/// </summary>
public interface IEmisorCredencialesProvider
{
    /// <summary>Carga las credenciales CSD del emisor por RFC.</summary>
    Task<EmisorCredenciales> ObtenerCredencialesAsync(string rfc, CancellationToken ct = default);

    /// <summary>Verifica si existen credenciales configuradas para el RFC.</summary>
    bool ExistenCredenciales(string rfc);
}

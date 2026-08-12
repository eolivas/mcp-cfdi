namespace McpCfdi.Infrastructure.Pac;

/// <summary>
/// Configuración de emisores. Los certificados se cargan desde disco por RFC.
/// El password de la llave privada se recibe vía variable de entorno.
/// Se enlaza a la sección "Emisores" del archivo de configuración.
/// </summary>
public class EmisoresOptions
{
    public const string SectionName = "Emisores";

    /// <summary>Directorio base donde se almacenan los certificados por RFC.</summary>
    public string CertificadosDir { get; set; } = "./certs/cfdi";

    /// <summary>RFC del emisor por defecto (usado cuando no se especifica en el request).</summary>
    public string DefaultRfc { get; set; } = string.Empty;
}

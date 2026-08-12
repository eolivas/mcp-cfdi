namespace McpCfdi.Infrastructure.Pac;

/// <summary>
/// Opciones de configuración del PAC (Proveedor Autorizado de Certificación).
/// Se enlaza a la sección "Pac" del archivo de configuración.
/// </summary>
public class PacOptions
{
    public const string SectionName = "Pac";

    /// <summary>Nombre del PAC activo (debe coincidir con una sección hija).</summary>
    public string ActiveProvider { get; set; } = "Multifacturas";

    /// <summary>Configuración del proveedor Multifacturas.</summary>
    public MultifacturasPacOptions Multifacturas { get; set; } = new();

    /// <summary>Configuración del proveedor FiscalCloud (opcional).</summary>
    public FiscalCloudPacOptions? FiscalCloud { get; set; }
}

/// <summary>
/// Opciones de configuración para el PAC Multifacturas.
/// </summary>
public class MultifacturasPacOptions
{
    /// <summary>URL base de la API de Multifacturas.</summary>
    public string BaseUrl { get; set; } = "https://api.multifacturas.com";

    /// <summary>Clave de API para autenticación.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Usuario para autenticación.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Contraseña para autenticación.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Timeout en segundos para las llamadas HTTP.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Opciones de configuración para el PAC FiscalCloud.
/// </summary>
public class FiscalCloudPacOptions
{
    /// <summary>URL base de la API de FiscalCloud.</summary>
    public string BaseUrl { get; set; } = "https://api.fiscalcloud.mx";

    /// <summary>Clave de API para autenticación.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Usuario para autenticación.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Contraseña para autenticación.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Timeout en segundos para las llamadas HTTP.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}

namespace McpCfdi.Infrastructure.Exceptions;

/// <summary>
/// Base abstracta para todas las excepciones relacionadas con operaciones PAC.
/// Incluye información del proveedor y códigos de error opcionales.
/// </summary>
public abstract class PacException : Exception
{
    /// <summary>Nombre del PAC que originó el error (ej: "Multifacturas").</summary>
    public string PacProvider { get; }

    /// <summary>Código de error devuelto por el PAC, si aplica.</summary>
    public string? CodigoError { get; }

    /// <summary>Detalle adicional del error devuelto por el PAC, si aplica.</summary>
    public string? DetalleError { get; }

    protected PacException(string message, string pacProvider = "", string? codigoError = null, string? detalleError = null)
        : base(message)
    {
        PacProvider = pacProvider;
        CodigoError = codigoError;
        DetalleError = detalleError;
    }

    protected PacException(string message, Exception innerException, string pacProvider = "", string? codigoError = null, string? detalleError = null)
        : base(message, innerException)
    {
        PacProvider = pacProvider;
        CodigoError = codigoError;
        DetalleError = detalleError;
    }
}

/// <summary>
/// Excepción para errores transitorios del PAC (HTTP 5xx, timeouts).
/// Estos errores son retriables mediante el decorator de resiliencia.
/// </summary>
public class PacTransientException : PacException
{
    public PacTransientException(string message, string pacProvider = "")
        : base(message, pacProvider)
    {
    }

    public PacTransientException(string message, Exception innerException, string pacProvider = "")
        : base(message, innerException, pacProvider)
    {
    }
}

/// <summary>
/// Excepción para errores de validación del PAC (HTTP 400).
/// El CFDI o los datos enviados no cumplen las reglas del SAT/PAC.
/// No es retriable — requiere corrección de los datos.
/// </summary>
public class PacValidationException : PacException
{
    public PacValidationException(string message, string pacProvider = "", string? codigoError = null, string? detalleError = null)
        : base(message, pacProvider, codigoError, detalleError)
    {
    }

    public PacValidationException(string message, Exception innerException, string pacProvider = "", string? codigoError = null, string? detalleError = null)
        : base(message, innerException, pacProvider, codigoError, detalleError)
    {
    }
}

/// <summary>
/// Excepción para errores de autenticación con el PAC (HTTP 401).
/// Las credenciales configuradas son inválidas o han expirado.
/// No es retriable — requiere actualizar credenciales.
/// </summary>
public class PacAuthenticationException : PacException
{
    public PacAuthenticationException(string message, string pacProvider = "")
        : base(message, pacProvider)
    {
    }

    public PacAuthenticationException(string message, Exception innerException, string pacProvider = "")
        : base(message, innerException, pacProvider)
    {
    }
}

/// <summary>
/// Excepción para saldo insuficiente de timbres (HTTP 402).
/// El PAC requiere recargar créditos antes de poder timbrar.
/// No es retriable — requiere acción administrativa.
/// </summary>
public class PacInsufficientCreditsException : PacException
{
    public PacInsufficientCreditsException(string message, string pacProvider = "")
        : base(message, pacProvider)
    {
    }

    public PacInsufficientCreditsException(string message, Exception innerException, string pacProvider = "")
        : base(message, innerException, pacProvider)
    {
    }
}

/// <summary>
/// Excepción para respuestas inesperadas del PAC.
/// Se lanza cuando el PAC retorna un código o formato no documentado.
/// </summary>
public class PacIntegrationException : PacException
{
    public PacIntegrationException(string message, string pacProvider = "")
        : base(message, pacProvider)
    {
    }

    public PacIntegrationException(string message, Exception innerException, string pacProvider = "")
        : base(message, innerException, pacProvider)
    {
    }
}

/// <summary>
/// Excepción lanzada cuando el circuit breaker está abierto.
/// Indica que el PAC ha sido marcado como no disponible tras fallos consecutivos.
/// </summary>
public class PacUnavailableException : PacException
{
    public PacUnavailableException(string message, string pacProvider = "")
        : base(message, pacProvider)
    {
    }

    public PacUnavailableException(string message, Exception innerException, string pacProvider = "")
        : base(message, innerException, pacProvider)
    {
    }
}

/// <summary>
/// Excepción para errores de credenciales del emisor (archivos CSD o variables de entorno faltantes).
/// No es una excepción PAC — es un error de configuración local del emisor.
/// </summary>
public class EmisorCredencialesException : Exception
{
    public EmisorCredencialesException(string message)
        : base(message)
    {
    }

    public EmisorCredencialesException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

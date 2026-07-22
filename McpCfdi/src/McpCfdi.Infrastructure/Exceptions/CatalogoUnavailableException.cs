namespace McpCfdi.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when SAT catalog data is unavailable (e.g., database unreachable).
/// Includes the name of the catalog that could not be validated.
/// </summary>
public class CatalogoUnavailableException : Exception
{
    /// <summary>
    /// The name of the catalog that was being accessed when the failure occurred.
    /// </summary>
    public string Catalogo { get; }

    public CatalogoUnavailableException(string catalogo, string message)
        : base(message)
    {
        Catalogo = catalogo;
    }

    public CatalogoUnavailableException(string catalogo, string message, Exception innerException)
        : base(message, innerException)
    {
        Catalogo = catalogo;
    }
}

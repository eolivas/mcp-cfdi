namespace McpCfdi.Domain.ValueObjects;

/// <summary>
/// Clave de un catálogo SAT. Valida formato no vacío.
/// La validación de existencia en catálogo se hace vía ICatalogoSatService.
/// </summary>
public sealed record ClaveCatalogo
{
    public string Catalogo { get; }
    public string Clave { get; }

    public ClaveCatalogo(string catalogo, string clave)
    {
        if (string.IsNullOrWhiteSpace(catalogo))
            throw new ArgumentException("Catalogo cannot be null or empty.", nameof(catalogo));

        if (string.IsNullOrWhiteSpace(clave))
            throw new ArgumentException("Clave cannot be null or empty.", nameof(clave));

        Catalogo = catalogo;
        Clave = clave;
    }

    public override string ToString() => $"{Catalogo}:{Clave}";
}

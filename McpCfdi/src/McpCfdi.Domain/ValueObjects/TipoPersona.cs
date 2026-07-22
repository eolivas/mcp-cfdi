namespace McpCfdi.Domain;

/// <summary>
/// Tipo de persona según la estructura del RFC.
/// </summary>
public enum TipoPersona
{
    /// <summary>Persona física — RFC de 13 caracteres.</summary>
    Fisica,

    /// <summary>Persona moral — RFC de 12 caracteres.</summary>
    Moral,

    /// <summary>RFC genérico (público en general o extranjeros).</summary>
    Generico
}

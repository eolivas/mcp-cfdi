namespace McpCfdi.Infrastructure.Persistence;

/// <summary>
/// Persistence model for SAT catalog entries.
/// Each row represents one entry in a SAT catalog (e.g., c_FormaPago, c_Moneda).
/// </summary>
public class CatalogoEntry
{
    public int Id { get; set; }
    public string NombreCatalogo { get; set; } = default!;
    public string Clave { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public DateTime? FechaInicioVigencia { get; set; }
    public DateTime? FechaFinVigencia { get; set; }
    public string? Metadata { get; set; }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using McpCfdi.Domain.Interfaces;
using McpCfdi.Infrastructure.Exceptions;
using McpCfdi.Infrastructure.Persistence;

namespace McpCfdi.Infrastructure.Catalogs;

/// <summary>
/// EF Core implementation of <see cref="ICatalogoSatService"/> that validates catalog keys
/// against the SAT catalog entries stored in PostgreSQL.
/// </summary>
public sealed class CatalogoSatService : ICatalogoSatService
{
    private readonly McpCfdiDbContext _dbContext;

    public CatalogoSatService(McpCfdiDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<bool> ExisteClaveAsync(
        string catalogo,
        string clave,
        DateTime? fechaEmision = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.CatalogoEntries
                .Where(e => e.NombreCatalogo == catalogo && e.Clave == clave);

            if (fechaEmision.HasValue)
            {
                var fecha = fechaEmision.Value;
                query = query.Where(e =>
                    (e.FechaInicioVigencia == null || e.FechaInicioVigencia <= fecha) &&
                    (e.FechaFinVigencia == null || e.FechaFinVigencia >= fecha));
            }

            return await query.AnyAsync(ct);
        }
        catch (Exception ex) when (IsDbUnavailable(ex))
        {
            throw new CatalogoUnavailableException(
                catalogo,
                $"No se pudo validar la clave '{clave}' en el catálogo '{catalogo}' porque la base de datos no está disponible.",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<CatalogoValidationResult> ValidarClavesAsync(
        IEnumerable<CatalogoValidationRequest> requests,
        DateTime fechaEmision,
        CancellationToken ct = default)
    {
        var failures = new List<CatalogoValidationFailure>();

        foreach (var request in requests)
        {
            var exists = await ExisteClaveAsync(request.Catalogo, request.Clave, fechaEmision, ct);

            if (!exists)
            {
                failures.Add(new CatalogoValidationFailure(request.Clave, request.Catalogo, request.CampoCfdi));
            }
        }

        return new CatalogoValidationResult(failures);
    }

    /// <inheritdoc />
    public async Task<int> ObtenerDecimalesMonedaAsync(string claveMoneda, CancellationToken ct = default)
    {
        try
        {
            var entry = await _dbContext.CatalogoEntries
                .Where(e => e.NombreCatalogo == "c_Moneda" && e.Clave == claveMoneda)
                .FirstOrDefaultAsync(ct);

            if (entry is null)
            {
                throw new CatalogoUnavailableException(
                    "c_Moneda",
                    $"No se encontró la moneda '{claveMoneda}' en el catálogo c_Moneda.");
            }

            if (string.IsNullOrWhiteSpace(entry.Metadata))
            {
                throw new CatalogoUnavailableException(
                    "c_Moneda",
                    $"La entrada de moneda '{claveMoneda}' no contiene metadata con información de decimales.");
            }

            using var document = JsonDocument.Parse(entry.Metadata);
            if (document.RootElement.TryGetProperty("Decimales", out var decimalesElement))
            {
                return decimalesElement.GetInt32();
            }

            throw new CatalogoUnavailableException(
                "c_Moneda",
                $"La metadata de la moneda '{claveMoneda}' no contiene la propiedad 'Decimales'.");
        }
        catch (CatalogoUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (IsDbUnavailable(ex))
        {
            throw new CatalogoUnavailableException(
                "c_Moneda",
                $"No se pudo obtener los decimales de la moneda '{claveMoneda}' porque la base de datos no está disponible.",
                ex);
        }
    }

    private static bool IsDbUnavailable(Exception ex)
    {
        return ex is InvalidOperationException
            || ex is TimeoutException
            || ex.GetType().FullName?.StartsWith("Npgsql.", StringComparison.Ordinal) == true
            || (ex.InnerException is not null && IsDbUnavailable(ex.InnerException));
    }
}

namespace McpCfdi.Domain.Interfaces;

/// <summary>
/// Port for validating keys against the official SAT catalogs.
/// Implementations may use EF Core against PostgreSQL or an in-memory store for tests.
/// </summary>
public interface ICatalogoSatService
{
    /// <summary>
    /// Checks whether a key exists in the specified SAT catalog, optionally checking validity for the given date.
    /// </summary>
    /// <param name="catalogo">Name of the SAT catalog (e.g., "c_FormaPago", "c_Moneda").</param>
    /// <param name="clave">The key value to look up.</param>
    /// <param name="fechaEmision">Optional emission date to verify catalog key is within its validity range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the key exists and is valid for the given date; false otherwise.</returns>
    Task<bool> ExisteClaveAsync(string catalogo, string clave, DateTime? fechaEmision = null, CancellationToken ct = default);

    /// <summary>
    /// Validates multiple catalog keys in a single call, evaluating all without short-circuiting on the first failure.
    /// </summary>
    /// <param name="requests">Collection of validation requests, each specifying a catalog, key, and CFDI field.</param>
    /// <param name="fechaEmision">Emission date to verify catalog keys are within their validity range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of validation failures, if any.</returns>
    Task<CatalogoValidationResult> ValidarClavesAsync(IEnumerable<CatalogoValidationRequest> requests, DateTime fechaEmision, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the number of decimal places defined for a currency in the SAT c_Moneda catalog.
    /// </summary>
    /// <param name="claveMoneda">Currency key (e.g., "MXN", "USD").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of decimal places for the currency.</returns>
    Task<int> ObtenerDecimalesMonedaAsync(string claveMoneda, CancellationToken ct = default);
}

/// <summary>
/// Represents a single catalog validation request specifying which catalog and key to validate.
/// </summary>
/// <param name="Catalogo">Name of the SAT catalog (e.g., "c_FormaPago").</param>
/// <param name="Clave">The key value to validate.</param>
/// <param name="CampoCfdi">The CFDI field where this key is used (for error reporting).</param>
public sealed record CatalogoValidationRequest(string Catalogo, string Clave, string CampoCfdi);

/// <summary>
/// Contains the result of a batch catalog validation, including all failures found.
/// </summary>
public sealed record CatalogoValidationResult
{
    /// <summary>
    /// The list of individual validation failures. Empty when all keys are valid.
    /// </summary>
    public IReadOnlyList<CatalogoValidationFailure> Failures { get; }

    /// <summary>
    /// True when all catalog keys passed validation.
    /// </summary>
    public bool IsValid => Failures.Count == 0;

    public CatalogoValidationResult(IReadOnlyList<CatalogoValidationFailure> failures)
    {
        Failures = failures ?? [];
    }
}

/// <summary>
/// Describes a single catalog validation failure.
/// </summary>
/// <param name="Clave">The invalid key value.</param>
/// <param name="Catalogo">The catalog against which validation failed.</param>
/// <param name="CampoCfdi">The CFDI field where the invalid key was found.</param>
public sealed record CatalogoValidationFailure(string Clave, string Catalogo, string CampoCfdi);

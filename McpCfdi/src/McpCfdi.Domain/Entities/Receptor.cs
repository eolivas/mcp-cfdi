using McpCfdi.Domain.Common;
using McpCfdi.Domain.ValueObjects;

namespace McpCfdi.Domain.Entities;

/// <summary>
/// Represents the Receptor (receiver) node of a CFDI 4.0 document.
/// Contains the RFC, name, fiscal address postal code, tax regime, and CFDI usage.
/// </summary>
public class Receptor : Entity<Guid>
{
    public Rfc Rfc { get; private set; }
    public string Nombre { get; private set; }
    public CodigoPostal DomicilioFiscalReceptor { get; private set; }
    public ClaveCatalogo RegimenFiscalReceptor { get; private set; }
    public ClaveCatalogo UsoCfdi { get; private set; }

    public Receptor(
        Rfc rfc,
        string nombre,
        CodigoPostal domicilioFiscalReceptor,
        ClaveCatalogo regimenFiscalReceptor,
        ClaveCatalogo usoCfdi)
    {
        ArgumentNullException.ThrowIfNull(rfc, nameof(rfc));
        ArgumentNullException.ThrowIfNull(domicilioFiscalReceptor, nameof(domicilioFiscalReceptor));
        ArgumentNullException.ThrowIfNull(regimenFiscalReceptor, nameof(regimenFiscalReceptor));
        ArgumentNullException.ThrowIfNull(usoCfdi, nameof(usoCfdi));

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("Nombre cannot be null, empty, or whitespace.", nameof(nombre));

        var trimmedNombre = nombre.Trim();

        if (trimmedNombre.Length < 1 || trimmedNombre.Length > 254)
            throw new ArgumentException("Nombre must be between 1 and 254 characters.", nameof(nombre));

        Id = Guid.NewGuid();
        Rfc = rfc;
        Nombre = trimmedNombre;
        DomicilioFiscalReceptor = domicilioFiscalReceptor;
        RegimenFiscalReceptor = regimenFiscalReceptor;
        UsoCfdi = usoCfdi;
    }
}

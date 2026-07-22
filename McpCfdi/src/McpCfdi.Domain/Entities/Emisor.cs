using McpCfdi.Domain.Common;
using McpCfdi.Domain.ValueObjects;

namespace McpCfdi.Domain.Entities;

/// <summary>
/// Represents the Emisor (issuer) node of a CFDI 4.0 document.
/// Contains the RFC, name, and tax regime of the entity issuing the invoice.
/// </summary>
public class Emisor : Entity<Guid>
{
    public Rfc Rfc { get; private set; }
    public string Nombre { get; private set; }
    public ClaveCatalogo RegimenFiscal { get; private set; }

    public Emisor(Rfc rfc, string nombre, ClaveCatalogo regimenFiscal)
    {
        ArgumentNullException.ThrowIfNull(rfc, nameof(rfc));
        ArgumentNullException.ThrowIfNull(regimenFiscal, nameof(regimenFiscal));

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("Nombre cannot be null, empty, or whitespace.", nameof(nombre));

        var trimmedNombre = nombre.Trim();

        if (trimmedNombre.Length < 1 || trimmedNombre.Length > 254)
            throw new ArgumentException("Nombre must be between 1 and 254 characters.", nameof(nombre));

        Id = Guid.NewGuid();
        Rfc = rfc;
        Nombre = trimmedNombre;
        RegimenFiscal = regimenFiscal;
    }
}

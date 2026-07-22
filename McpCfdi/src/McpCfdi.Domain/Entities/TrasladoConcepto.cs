using McpCfdi.Domain.Common;
using McpCfdi.Domain.ValueObjects;

namespace McpCfdi.Domain.Entities;

/// <summary>
/// Represents a tax transfer (traslado) line within a CFDI 4.0 Concepto node.
/// Enforces SAT rules: if TipoFactor is "Exento", TasaOCuota and Importe must be null.
/// If TipoFactor is "Tasa" or "Cuota", TasaOCuota and Importe are required.
/// </summary>
public class TrasladoConcepto : Entity<Guid>
{
    private static readonly string[] ValidTipoFactorValues = ["Tasa", "Cuota", "Exento"];

    public MontoMoneda Base { get; private set; }
    public ClaveCatalogo Impuesto { get; private set; }
    public ClaveCatalogo TipoFactor { get; private set; }
    public decimal? TasaOCuota { get; private set; }
    public MontoMoneda? Importe { get; private set; }

    public TrasladoConcepto(
        MontoMoneda @base,
        ClaveCatalogo impuesto,
        ClaveCatalogo tipoFactor,
        decimal? tasaOCuota,
        MontoMoneda? importe)
    {
        ArgumentNullException.ThrowIfNull(@base, nameof(@base));
        ArgumentNullException.ThrowIfNull(impuesto, nameof(impuesto));
        ArgumentNullException.ThrowIfNull(tipoFactor, nameof(tipoFactor));

        if (!ValidTipoFactorValues.Contains(tipoFactor.Clave))
            throw new ArgumentException(
                $"TipoFactor must be one of: Tasa, Cuota, Exento. Got: '{tipoFactor.Clave}'.",
                nameof(tipoFactor));

        if (tipoFactor.Clave == "Exento")
        {
            if (tasaOCuota is not null)
                throw new ArgumentException(
                    "TasaOCuota must be null when TipoFactor is 'Exento'.",
                    nameof(tasaOCuota));

            if (importe is not null)
                throw new ArgumentException(
                    "Importe must be null when TipoFactor is 'Exento'.",
                    nameof(importe));
        }
        else
        {
            if (tasaOCuota is null)
                throw new ArgumentException(
                    $"TasaOCuota is required when TipoFactor is '{tipoFactor.Clave}'.",
                    nameof(tasaOCuota));

            if (importe is null)
                throw new ArgumentException(
                    $"Importe is required when TipoFactor is '{tipoFactor.Clave}'.",
                    nameof(importe));
        }

        Id = Guid.NewGuid();
        Base = @base;
        Impuesto = impuesto;
        TipoFactor = tipoFactor;
        TasaOCuota = tasaOCuota;
        Importe = importe;
    }
}

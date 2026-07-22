namespace McpCfdi.Domain.ValueObjects;

/// <summary>
/// Representa un traslado global de impuestos en el CFDI.
/// Agrupa por combinación única de (Impuesto, TipoFactor, TasaOCuota).
/// Si TipoFactor es "Exento", TasaOCuota e Importe deben ser null.
/// </summary>
public sealed record TrasladoGlobal
{
    public MontoMoneda Base { get; }
    public ClaveCatalogo Impuesto { get; }
    public ClaveCatalogo TipoFactor { get; }
    public decimal? TasaOCuota { get; }
    public MontoMoneda? Importe { get; }

    public TrasladoGlobal(
        MontoMoneda @base,
        ClaveCatalogo impuesto,
        ClaveCatalogo tipoFactor,
        decimal? tasaOCuota,
        MontoMoneda? importe)
    {
        ArgumentNullException.ThrowIfNull(@base);
        ArgumentNullException.ThrowIfNull(impuesto);
        ArgumentNullException.ThrowIfNull(tipoFactor);

        if (tipoFactor.Clave == "Exento")
        {
            if (tasaOCuota is not null)
                throw new ArgumentException(
                    "TasaOCuota must be null when TipoFactor is 'Exento'.", nameof(tasaOCuota));

            if (importe is not null)
                throw new ArgumentException(
                    "Importe must be null when TipoFactor is 'Exento'.", nameof(importe));
        }

        Base = @base;
        Impuesto = impuesto;
        TipoFactor = tipoFactor;
        TasaOCuota = tasaOCuota;
        Importe = importe;
    }
}

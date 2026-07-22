namespace McpCfdi.Domain.ValueObjects;

/// <summary>
/// Nodo global de impuestos del CFDI.
/// Se incluye cuando al menos un concepto tiene ObjetoImp="02".
/// Contiene el total de impuestos trasladados y la lista de traslados globales.
/// </summary>
public sealed record ImpuestosGlobal
{
    public MontoMoneda TotalImpuestosTrasladados { get; }
    public IReadOnlyList<TrasladoGlobal> Traslados { get; }

    public ImpuestosGlobal(MontoMoneda totalImpuestosTrasladados, IReadOnlyList<TrasladoGlobal> traslados)
    {
        ArgumentNullException.ThrowIfNull(totalImpuestosTrasladados);
        ArgumentNullException.ThrowIfNull(traslados);

        if (traslados.Count == 0)
            throw new ArgumentException("Traslados must contain at least one item.", nameof(traslados));

        TotalImpuestosTrasladados = totalImpuestosTrasladados;
        Traslados = traslados;
    }
}

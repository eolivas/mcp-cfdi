using McpCfdi.Domain.Exceptions;

namespace McpCfdi.Domain.ValueObjects;

/// <summary>
/// Monto monetario con redondeo conforme a decimales de la moneda SAT.
/// Extiende Money existente agregando redondeo currency-aware.
/// </summary>
public sealed record MontoMoneda
{
    public decimal Valor { get; }
    public int Decimales { get; }

    public MontoMoneda(decimal valor, int decimales)
    {
        if (valor < 0)
            throw new InvalidMontoException(valor);

        Decimales = decimales;
        Valor = Math.Round(valor, decimales, MidpointRounding.AwayFromZero);
    }

    public static MontoMoneda operator +(MontoMoneda a, MontoMoneda b)
    {
        if (a.Decimales != b.Decimales)
            throw new InvalidOperationException("No se pueden sumar montos con diferente precisión decimal.");
        return new MontoMoneda(a.Valor + b.Valor, a.Decimales);
    }

    public static MontoMoneda operator *(MontoMoneda monto, decimal factor)
        => new(monto.Valor * factor, monto.Decimales);

    /// <summary>
    /// Formats the value for XML serialization with exact decimal places
    /// as defined by the currency (e.g., "100.00" for 2 decimals).
    /// </summary>
    public string FormatearParaXml() => Valor.ToString($"F{Decimales}");
}

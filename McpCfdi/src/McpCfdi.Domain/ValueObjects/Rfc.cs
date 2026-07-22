using System.Text.RegularExpressions;
using McpCfdi.Domain.Exceptions;

namespace McpCfdi.Domain;

/// <summary>
/// RFC (Registro Federal de Contribuyentes) con validación según reglas del SAT.
/// Personas morales: 12 caracteres. Personas físicas: 13 caracteres.
/// Admite RFC genéricos: XAXX010101000 (público general), XEXX010101000 (extranjeros).
/// </summary>
public sealed partial record Rfc
{
    private static readonly HashSet<string> RfcGenericos = new()
    {
        "XAXX010101000", "XEXX010101000"
    };

    public string Valor { get; }
    public TipoPersona TipoPersona { get; }

    public Rfc(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new InvalidRfcException(valor ?? string.Empty);

        var normalized = valor.Trim().ToUpperInvariant();

        if (RfcGenericos.Contains(normalized))
        {
            Valor = normalized;
            TipoPersona = TipoPersona.Generico;
            return;
        }

        if (normalized.Length == 12 && PersonaMoralRegex().IsMatch(normalized))
        {
            Valor = normalized;
            TipoPersona = TipoPersona.Moral;
            return;
        }

        if (normalized.Length == 13 && PersonaFisicaRegex().IsMatch(normalized))
        {
            Valor = normalized;
            TipoPersona = TipoPersona.Fisica;
            return;
        }

        throw new InvalidRfcException(valor);
    }

    /// <summary>
    /// Indica si el RFC es uno de los valores genéricos del SAT.
    /// </summary>
    public bool EsGenerico => RfcGenericos.Contains(Valor);

    public override string ToString() => Valor;

    [GeneratedRegex(@"^[A-ZÑ&]{3}\d{6}[A-Z0-9]{3}$")]
    private static partial Regex PersonaMoralRegex();

    [GeneratedRegex(@"^[A-ZÑ&]{4}\d{6}[A-Z0-9]{3}$")]
    private static partial Regex PersonaFisicaRegex();
}

using System.Text.RegularExpressions;

namespace McpCfdi.Domain.ValueObjects;

/// <summary>
/// Código postal de 5 dígitos numéricos.
/// </summary>
public sealed partial record CodigoPostal
{
    public string Valor { get; }

    public CodigoPostal(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !CodigoPostalRegex().IsMatch(valor))
            throw new ArgumentException(
                "El código postal debe ser exactamente 5 dígitos numéricos.", nameof(valor));

        Valor = valor;
    }

    [GeneratedRegex(@"^\d{5}$")]
    private static partial Regex CodigoPostalRegex();
}

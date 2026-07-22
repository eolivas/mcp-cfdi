namespace McpCfdi.Domain.Exceptions;

/// <summary>
/// Exception thrown when an RFC value does not comply with SAT structure rules.
/// </summary>
public class InvalidRfcException : Exception
{
    public string Valor { get; }

    public InvalidRfcException(string valor)
        : base($"El RFC '{valor}' no cumple con la estructura definida por el SAT.")
    {
        Valor = valor;
    }

    public InvalidRfcException(string valor, string message)
        : base(message)
    {
        Valor = valor;
    }

    public InvalidRfcException(string valor, string message, Exception innerException)
        : base(message, innerException)
    {
        Valor = valor;
    }
}

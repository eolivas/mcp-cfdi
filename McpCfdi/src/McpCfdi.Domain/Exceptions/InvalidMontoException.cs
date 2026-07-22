namespace McpCfdi.Domain.Exceptions;

/// <summary>
/// Exception thrown when a monetary amount violates domain rules
/// (e.g., negative values).
/// </summary>
public class InvalidMontoException : Exception
{
    public decimal ValorInvalido { get; }

    public InvalidMontoException(decimal valorInvalido)
        : base($"El monto '{valorInvalido}' no es válido. Los montos no pueden ser negativos.")
    {
        ValorInvalido = valorInvalido;
    }

    public InvalidMontoException(decimal valorInvalido, string message)
        : base(message)
    {
        ValorInvalido = valorInvalido;
    }

    public InvalidMontoException(decimal valorInvalido, string message, Exception innerException)
        : base(message, innerException)
    {
        ValorInvalido = valorInvalido;
    }
}

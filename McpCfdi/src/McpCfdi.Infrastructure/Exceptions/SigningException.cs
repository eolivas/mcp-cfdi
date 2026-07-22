namespace McpCfdi.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when digital signing operations fail.
/// Covers invalid key format, decryption failures, or invalid certificate format.
/// </summary>
public class SigningException : Exception
{
    public SigningException(string message)
        : base(message)
    {
    }

    public SigningException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

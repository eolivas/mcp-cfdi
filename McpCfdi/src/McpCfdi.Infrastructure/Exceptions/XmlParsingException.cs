namespace McpCfdi.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when XML parsing fails during CFDI deserialization.
/// Covers malformed XML, wrong namespace, or missing mandatory attributes.
/// </summary>
public class XmlParsingException : Exception
{
    public XmlParsingException(string message)
        : base(message)
    {
    }

    public XmlParsingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

namespace McpCfdi.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when the XSLT transformation to generate the cadena original fails.
/// </summary>
public class XsltTransformException : Exception
{
    public XsltTransformException(string message)
        : base(message)
    {
    }

    public XsltTransformException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

namespace McpCfdi.Domain.Exceptions;

/// <summary>
/// Exception thrown when a mandatory field is missing or does not comply
/// with validation rules during CFDI generation.
/// </summary>
public class MissingMandatoryFieldException : Exception
{
    public string FieldName { get; }
    public string? EntityName { get; }

    public MissingMandatoryFieldException(string fieldName)
        : base($"El campo obligatorio '{fieldName}' no fue proporcionado o no cumple con las reglas de validación.")
    {
        FieldName = fieldName;
    }

    public MissingMandatoryFieldException(string fieldName, string entityName)
        : base($"El campo obligatorio '{fieldName}' del nodo '{entityName}' no fue proporcionado o no cumple con las reglas de validación.")
    {
        FieldName = fieldName;
        EntityName = entityName;
    }

    public MissingMandatoryFieldException(string fieldName, string entityName, string message)
        : base(message)
    {
        FieldName = fieldName;
        EntityName = entityName;
    }

    public MissingMandatoryFieldException(string fieldName, string entityName, string message, Exception innerException)
        : base(message, innerException)
    {
        FieldName = fieldName;
        EntityName = entityName;
    }
}

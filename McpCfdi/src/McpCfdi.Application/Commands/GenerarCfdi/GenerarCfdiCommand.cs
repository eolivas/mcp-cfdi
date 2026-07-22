using McpCfdi.Application.DTOs;
using MediatR;

namespace McpCfdi.Application.Commands.GenerarCfdi;

/// <summary>
/// Comando para generar un CFDI 4.0 de tipo Ingreso.
/// Contiene toda la información necesaria para construir, validar y firmar el comprobante.
/// </summary>
public record GenerarCfdiCommand : IRequest<CfdiResult>
{
    /// <summary>Datos fiscales del emisor del comprobante.</summary>
    public required EmisorDto Emisor { get; init; }

    /// <summary>Datos fiscales del receptor del comprobante.</summary>
    public required ReceptorDto Receptor { get; init; }

    /// <summary>Lista de conceptos (líneas de detalle) del comprobante. Debe contener al menos un elemento.</summary>
    public required IReadOnlyList<ConceptoDto> Conceptos { get; init; }

    /// <summary>Clave del catálogo c_FormaPago del SAT (e.g., "01" efectivo, "03" transferencia).</summary>
    public required string FormaPago { get; init; }

    /// <summary>Clave del catálogo c_MetodoPago: "PUE" (pago en una exhibición) o "PPD" (pago en parcialidades).</summary>
    public required string MetodoPago { get; init; }

    /// <summary>Clave del catálogo c_Moneda del SAT (e.g., "MXN", "USD").</summary>
    public required string Moneda { get; init; }

    /// <summary>Código postal del lugar de expedición (5 dígitos numéricos).</summary>
    public required string LugarExpedicion { get; init; }

    /// <summary>Clave del catálogo c_Exportacion (e.g., "01" no aplica, "02" definitiva).</summary>
    public required string Exportacion { get; init; }

    /// <summary>Fecha y hora de emisión del comprobante. Si es null, se asigna la fecha/hora actual.</summary>
    public DateTime? Fecha { get; init; }

    /// <summary>Llave privada del CSD en formato DER (bytes). Opcional si se firma externamente.</summary>
    public byte[]? LlavePrivadaDer { get; init; }

    /// <summary>Contraseña de la llave privada del CSD.</summary>
    public string? PasswordLlave { get; init; }

    /// <summary>Certificado del CSD en formato DER (bytes). Opcional si se firma externamente.</summary>
    public byte[]? CertificadoDer { get; init; }
}

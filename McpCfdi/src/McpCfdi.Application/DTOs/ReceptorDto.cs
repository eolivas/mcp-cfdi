namespace McpCfdi.Application.DTOs;

/// <summary>
/// Datos del receptor del CFDI.
/// </summary>
/// <param name="Rfc">RFC del receptor (12, 13 caracteres o RFC genérico).</param>
/// <param name="Nombre">Nombre o razón social del receptor.</param>
/// <param name="DomicilioFiscalReceptor">Código postal del domicilio fiscal del receptor (5 dígitos).</param>
/// <param name="RegimenFiscalReceptor">Clave del catálogo c_RegimenFiscal del SAT aplicable al receptor.</param>
/// <param name="UsoCfdi">Clave del catálogo c_UsoCFDI que indica el uso que el receptor dará al comprobante.</param>
public record ReceptorDto(
    string Rfc,
    string Nombre,
    string DomicilioFiscalReceptor,
    string RegimenFiscalReceptor,
    string UsoCfdi);

namespace McpCfdi.Application.DTOs;

/// <summary>
/// Datos del emisor del CFDI.
/// </summary>
/// <param name="Rfc">RFC del emisor (12 o 13 caracteres).</param>
/// <param name="Nombre">Nombre o razón social del emisor.</param>
/// <param name="RegimenFiscal">Clave del catálogo c_RegimenFiscal del SAT.</param>
public record EmisorDto(string Rfc, string Nombre, string RegimenFiscal);

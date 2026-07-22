namespace McpCfdi.Application.DTOs;

/// <summary>
/// Resultado de la generación exitosa de un CFDI.
/// </summary>
/// <param name="Xml">Documento XML del CFDI serializado.</param>
/// <param name="CadenaOriginal">Cadena original generada por la transformación XSLT oficial.</param>
/// <param name="Sello">Sello digital en Base64 (SHA-256 con RSA sobre la cadena original).</param>
/// <param name="ComprobanteId">Identificador único del comprobante generado.</param>
public record CfdiResult(string Xml, string CadenaOriginal, string Sello, Guid ComprobanteId);

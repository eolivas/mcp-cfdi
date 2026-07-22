namespace McpCfdi.Application.DTOs;

/// <summary>
/// Datos de un impuesto trasladado a nivel de concepto.
/// </summary>
/// <param name="Impuesto">Clave del catálogo c_Impuesto del SAT (e.g., "002" para IVA).</param>
/// <param name="TipoFactor">Tipo de factor: "Tasa", "Cuota" o "Exento".</param>
/// <param name="TasaOCuota">Valor de la tasa o cuota del catálogo c_TasaOCuota. Null cuando TipoFactor es "Exento".</param>
public record TrasladoDto(string Impuesto, string TipoFactor, decimal? TasaOCuota);

namespace McpCfdi.Application.DTOs;

/// <summary>
/// Datos de un concepto (línea de detalle) del CFDI.
/// </summary>
/// <param name="ClaveProdServ">Clave del catálogo c_ClaveProdServ del SAT.</param>
/// <param name="Cantidad">Cantidad del bien o servicio (mayor a cero, máximo 6 decimales).</param>
/// <param name="ClaveUnidad">Clave del catálogo c_ClaveUnidad del SAT.</param>
/// <param name="Descripcion">Descripción del bien o servicio (1-1000 caracteres).</param>
/// <param name="ValorUnitario">Valor unitario del bien o servicio.</param>
/// <param name="ObjetoImp">Clave del catálogo c_ObjetoImp ("01", "02" o "03").</param>
/// <param name="NoIdentificacion">Número de identificación interno (opcional, máximo 100 caracteres).</param>
/// <param name="Unidad">Descripción de la unidad de medida (opcional, máximo 20 caracteres).</param>
/// <param name="Descuento">Monto de descuento aplicable al concepto (opcional).</param>
/// <param name="Traslados">Lista de impuestos trasladados del concepto (opcional).</param>
public record ConceptoDto(
    string ClaveProdServ,
    decimal Cantidad,
    string ClaveUnidad,
    string Descripcion,
    decimal ValorUnitario,
    string ObjetoImp,
    string? NoIdentificacion = null,
    string? Unidad = null,
    decimal? Descuento = null,
    IReadOnlyList<TrasladoDto>? Traslados = null);

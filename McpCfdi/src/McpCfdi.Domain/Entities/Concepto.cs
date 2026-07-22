using McpCfdi.Domain.Common;
using McpCfdi.Domain.ValueObjects;

namespace McpCfdi.Domain.Entities;

/// <summary>
/// Represents a line item (Concepto) within a CFDI 4.0 Comprobante node.
/// Enforces SAT validation rules for mandatory/optional attributes,
/// decimal precision, and string length constraints.
/// </summary>
public class Concepto : Entity<Guid>
{
    public ClaveCatalogo ClaveProdServ { get; private set; }
    public decimal Cantidad { get; private set; }
    public ClaveCatalogo ClaveUnidad { get; private set; }
    public string Descripcion { get; private set; }
    public MontoMoneda ValorUnitario { get; private set; }
    public MontoMoneda Importe { get; private set; }
    public ClaveCatalogo ObjetoImp { get; private set; }
    public string? NoIdentificacion { get; private set; }
    public string? Unidad { get; private set; }
    public MontoMoneda? Descuento { get; private set; }

    private readonly List<TrasladoConcepto> _traslados = [];
    public IReadOnlyList<TrasladoConcepto> Traslados => _traslados.AsReadOnly();

    public Concepto(
        ClaveCatalogo claveProdServ,
        decimal cantidad,
        ClaveCatalogo claveUnidad,
        string descripcion,
        MontoMoneda valorUnitario,
        MontoMoneda importe,
        ClaveCatalogo objetoImp,
        string? noIdentificacion = null,
        string? unidad = null,
        MontoMoneda? descuento = null)
    {
        ArgumentNullException.ThrowIfNull(claveProdServ, nameof(claveProdServ));
        ArgumentNullException.ThrowIfNull(claveUnidad, nameof(claveUnidad));
        ArgumentNullException.ThrowIfNull(valorUnitario, nameof(valorUnitario));
        ArgumentNullException.ThrowIfNull(importe, nameof(importe));
        ArgumentNullException.ThrowIfNull(objetoImp, nameof(objetoImp));

        if (cantidad <= 0)
            throw new ArgumentException("Cantidad must be greater than 0.", nameof(cantidad));

        if (GetDecimalPlaces(cantidad) > 6)
            throw new ArgumentException("Cantidad must have at most 6 decimal places.", nameof(cantidad));

        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ArgumentException("Descripcion must not be null, empty, or whitespace-only.", nameof(descripcion));

        var trimmedDescripcion = descripcion.Trim();
        if (trimmedDescripcion.Length > 1000)
            throw new ArgumentException("Descripcion must be between 1 and 1000 characters.", nameof(descripcion));

        if (noIdentificacion is not null && noIdentificacion.Length > 100)
            throw new ArgumentException("NoIdentificacion must be at most 100 characters.", nameof(noIdentificacion));

        if (unidad is not null && unidad.Length > 20)
            throw new ArgumentException("Unidad must be at most 20 characters.", nameof(unidad));

        Id = Guid.NewGuid();
        ClaveProdServ = claveProdServ;
        Cantidad = cantidad;
        ClaveUnidad = claveUnidad;
        Descripcion = trimmedDescripcion;
        ValorUnitario = valorUnitario;
        Importe = importe;
        ObjetoImp = objetoImp;
        NoIdentificacion = noIdentificacion;
        Unidad = unidad;
        Descuento = descuento;
    }

    /// <summary>
    /// Adds a tax transfer (traslado) to this concept.
    /// </summary>
    public void AgregarTraslado(TrasladoConcepto traslado)
    {
        ArgumentNullException.ThrowIfNull(traslado, nameof(traslado));
        _traslados.Add(traslado);
    }

    private static int GetDecimalPlaces(decimal value)
    {
        // Normalize to remove trailing zeros, then count decimal digits
        value = value / 1.000000000000000000000000000000000m;
        var text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var dotIndex = text.IndexOf('.');
        return dotIndex < 0 ? 0 : text.Length - dotIndex - 1;
    }
}

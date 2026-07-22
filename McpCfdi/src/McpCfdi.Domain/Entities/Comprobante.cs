using System.Text.RegularExpressions;
using McpCfdi.Domain.Common;
using McpCfdi.Domain.ValueObjects;

namespace McpCfdi.Domain.Entities;

/// <summary>
/// Aggregate root representing a CFDI 4.0 Comprobante (tax receipt).
/// Created exclusively via the <see cref="Crear"/> factory method.
/// </summary>
public partial class Comprobante : AggregateRoot<Guid>
{
    // Atributos obligatorios del nodo raíz
    public DateTime Fecha { get; private set; }
    public ClaveCatalogo FormaPago { get; private set; } = null!;
    public string NoCertificado { get; private set; } = string.Empty;
    public string Certificado { get; private set; } = string.Empty;
    public MontoMoneda SubTotal { get; private set; } = null!;
    public ClaveCatalogo Moneda { get; private set; } = null!;
    public MontoMoneda Total { get; private set; } = null!;
    public ClaveCatalogo TipoDeComprobante { get; private set; } = null!;
    public ClaveCatalogo MetodoPago { get; private set; } = null!;
    public CodigoPostal LugarExpedicion { get; private set; } = null!;
    public ClaveCatalogo Exportacion { get; private set; } = null!;
    public string? Sello { get; private set; }

    // Atributos opcionales
    public MontoMoneda? Descuento { get; private set; }

    // Entidades hijas
    public Emisor Emisor { get; private set; } = null!;
    public Receptor Receptor { get; private set; } = null!;
    private readonly List<Concepto> _conceptos = [];
    public IReadOnlyList<Concepto> Conceptos => _conceptos.AsReadOnly();
    public ImpuestosGlobal? Impuestos { get; private set; }

    private Comprobante() { }

    /// <summary>
    /// Factory method to create a new Comprobante.
    /// SubTotal, Total, Descuento, and Impuestos are NOT set here — call <see cref="CalcularTotales"/> after creation.
    /// Sello, Certificado, and NoCertificado are initially empty — call <see cref="AsignarSello"/> to set them.
    /// </summary>
    public static Comprobante Crear(
        DateTime fecha,
        ClaveCatalogo formaPago,
        ClaveCatalogo moneda,
        ClaveCatalogo tipoDeComprobante,
        ClaveCatalogo metodoPago,
        CodigoPostal lugarExpedicion,
        ClaveCatalogo exportacion,
        Emisor emisor,
        Receptor receptor,
        List<Concepto> conceptos)
    {
        ArgumentNullException.ThrowIfNull(formaPago, nameof(formaPago));
        ArgumentNullException.ThrowIfNull(moneda, nameof(moneda));
        ArgumentNullException.ThrowIfNull(tipoDeComprobante, nameof(tipoDeComprobante));
        ArgumentNullException.ThrowIfNull(metodoPago, nameof(metodoPago));
        ArgumentNullException.ThrowIfNull(lugarExpedicion, nameof(lugarExpedicion));
        ArgumentNullException.ThrowIfNull(exportacion, nameof(exportacion));
        ArgumentNullException.ThrowIfNull(emisor, nameof(emisor));
        ArgumentNullException.ThrowIfNull(receptor, nameof(receptor));
        ArgumentNullException.ThrowIfNull(conceptos, nameof(conceptos));

        if (conceptos.Count == 0)
            throw new ArgumentException("Se requiere al menos un Concepto.", nameof(conceptos));

        var comprobante = new Comprobante
        {
            Id = Guid.NewGuid(),
            Fecha = fecha,
            FormaPago = formaPago,
            Moneda = moneda,
            TipoDeComprobante = tipoDeComprobante,
            MetodoPago = metodoPago,
            LugarExpedicion = lugarExpedicion,
            Exportacion = exportacion,
            Emisor = emisor,
            Receptor = receptor
        };

        comprobante._conceptos.AddRange(conceptos);

        return comprobante;
    }

    /// <summary>
    /// Computes SubTotal, Descuento, global Impuestos, and Total from the conceptos.
    /// Must be called after creating the Comprobante and before serializing to XML.
    /// </summary>
    /// <param name="decimalesMoneda">Number of decimal places for the currency (e.g., 2 for MXN).</param>
    public void CalcularTotales(int decimalesMoneda)
    {
        // 7.1: SubTotal = sum of all Concepto.Importe
        var subTotalValor = _conceptos.Sum(c => c.Importe.Valor);
        SubTotal = new MontoMoneda(subTotalValor, decimalesMoneda);

        // 7.2: Descuento = sum of all Concepto.Descuento (when present)
        var conceptosConDescuento = _conceptos.Where(c => c.Descuento is not null).ToList();
        if (conceptosConDescuento.Count > 0)
        {
            var descuentoValor = conceptosConDescuento.Sum(c => c.Descuento!.Valor);
            Descuento = new MontoMoneda(descuentoValor, decimalesMoneda);
        }
        else
        {
            Descuento = null;
        }

        // 6.1/6.2: Calculate global taxes
        var conceptosConImpuestos = _conceptos
            .Where(c => c.ObjetoImp.Clave == "02")
            .ToList();

        if (conceptosConImpuestos.Count > 0)
        {
            var todosTraslados = conceptosConImpuestos
                .SelectMany(c => c.Traslados)
                .ToList();

            var grupos = todosTraslados
                .GroupBy(t => new { ImpuestoClave = t.Impuesto.Clave, TipoFactorClave = t.TipoFactor.Clave, t.TasaOCuota })
                .ToList();

            var trasladosGlobales = new List<TrasladoGlobal>();

            foreach (var grupo in grupos)
            {
                var baseSum = grupo.Sum(t => t.Base.Valor);
                var baseMonto = new MontoMoneda(baseSum, decimalesMoneda);

                // Use the first element to get full ClaveCatalogo references
                var primerTraslado = grupo.First();

                MontoMoneda? importeMonto = null;
                if (primerTraslado.TipoFactor.Clave != "Exento")
                {
                    var importeSum = grupo.Sum(t => t.Importe!.Valor);
                    importeMonto = new MontoMoneda(importeSum, decimalesMoneda);
                }

                trasladosGlobales.Add(new TrasladoGlobal(
                    baseMonto,
                    primerTraslado.Impuesto,
                    primerTraslado.TipoFactor,
                    primerTraslado.TasaOCuota,
                    importeMonto));
            }

            // TotalImpuestosTrasladados = sum of Importe for non-Exento groups
            var totalTrasladados = trasladosGlobales
                .Where(t => t.TipoFactor.Clave != "Exento")
                .Sum(t => t.Importe!.Valor);

            var totalTrasladosMonto = new MontoMoneda(totalTrasladados, decimalesMoneda);

            Impuestos = new ImpuestosGlobal(totalTrasladosMonto, trasladosGlobales.AsReadOnly());
        }
        else
        {
            Impuestos = null;
        }

        // 7.3: Total = SubTotal - Descuento + TotalImpuestosTrasladados
        var totalValor = SubTotal.Valor
            - (Descuento?.Valor ?? 0m)
            + (Impuestos?.TotalImpuestosTrasladados.Valor ?? 0m);

        Total = new MontoMoneda(totalValor, decimalesMoneda);
    }

    /// <summary>
    /// Assigns the digital seal, certificate, and certificate number after signing.
    /// </summary>
    public void AsignarSello(string sello, string certificado, string noCertificado)
    {
        if (string.IsNullOrWhiteSpace(sello))
            throw new ArgumentException("Sello no puede ser nulo o vacío.", nameof(sello));

        if (string.IsNullOrWhiteSpace(certificado))
            throw new ArgumentException("Certificado no puede ser nulo o vacío.", nameof(certificado));

        if (string.IsNullOrWhiteSpace(noCertificado))
            throw new ArgumentException("NoCertificado no puede ser nulo o vacío.", nameof(noCertificado));

        if (!NoCertificadoRegex().IsMatch(noCertificado))
            throw new ArgumentException("NoCertificado debe ser exactamente 20 dígitos numéricos.", nameof(noCertificado));

        Sello = sello;
        Certificado = certificado;
        NoCertificado = noCertificado;
    }

    [GeneratedRegex(@"^\d{20}$")]
    private static partial Regex NoCertificadoRegex();
}

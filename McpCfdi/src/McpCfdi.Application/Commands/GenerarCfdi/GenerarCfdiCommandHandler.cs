using McpCfdi.Application.DTOs;
using McpCfdi.Domain;
using McpCfdi.Domain.Entities;
using McpCfdi.Domain.Interfaces;
using McpCfdi.Domain.ValueObjects;
using MediatR;

namespace McpCfdi.Application.Commands.GenerarCfdi;

/// <summary>
/// Orquesta la generación de un CFDI 4.0: construye el modelo de dominio,
/// calcula totales, serializa a XML, genera cadena original, firma y retorna el resultado.
/// </summary>
public class GenerarCfdiCommandHandler : IRequestHandler<GenerarCfdiCommand, CfdiResult>
{
    private readonly ICatalogoSatService _catalogoSatService;
    private readonly ICfdiSerializer _cfdiSerializer;
    private readonly ICadenaOriginalGenerator _cadenaOriginalGenerator;
    private readonly ISelloDigitalService _selloDigitalService;

    public GenerarCfdiCommandHandler(
        ICatalogoSatService catalogoSatService,
        ICfdiSerializer cfdiSerializer,
        ICadenaOriginalGenerator cadenaOriginalGenerator,
        ISelloDigitalService selloDigitalService)
    {
        _catalogoSatService = catalogoSatService;
        _cfdiSerializer = cfdiSerializer;
        _cadenaOriginalGenerator = cadenaOriginalGenerator;
        _selloDigitalService = selloDigitalService;
    }

    public async Task<CfdiResult> Handle(GenerarCfdiCommand command, CancellationToken cancellationToken)
    {
        // 1. Obtener decimales de la moneda para redondeo correcto
        var decimalesMoneda = await _catalogoSatService.ObtenerDecimalesMonedaAsync(
            command.Moneda, cancellationToken);

        // 2. Construir modelo de dominio
        var emisor = new Emisor(
            new Rfc(command.Emisor.Rfc),
            command.Emisor.Nombre,
            new ClaveCatalogo("c_RegimenFiscal", command.Emisor.RegimenFiscal));

        var receptor = new Receptor(
            new Rfc(command.Receptor.Rfc),
            command.Receptor.Nombre,
            new CodigoPostal(command.Receptor.DomicilioFiscalReceptor),
            new ClaveCatalogo("c_RegimenFiscal", command.Receptor.RegimenFiscalReceptor),
            new ClaveCatalogo("c_UsoCFDI", command.Receptor.UsoCfdi));

        var conceptos = BuildConceptos(command.Conceptos, decimalesMoneda);

        var fecha = command.Fecha ?? DateTime.Now;

        var comprobante = Comprobante.Crear(
            fecha,
            new ClaveCatalogo("c_FormaPago", command.FormaPago),
            new ClaveCatalogo("c_Moneda", command.Moneda),
            new ClaveCatalogo("c_TipoDeComprobante", "I"), // Siempre Ingreso
            new ClaveCatalogo("c_MetodoPago", command.MetodoPago),
            new CodigoPostal(command.LugarExpedicion),
            new ClaveCatalogo("c_Exportacion", command.Exportacion),
            emisor,
            receptor,
            conceptos);

        // 3. Calcular totales (SubTotal, Descuento, Impuestos, Total)
        comprobante.CalcularTotales(decimalesMoneda);

        // 4. Serializar a XML
        var xml = _cfdiSerializer.Serializar(comprobante);

        // 5. Generar cadena original
        var cadenaOriginal = _cadenaOriginalGenerator.Generar(xml);

        // 6-8. Firmar y asignar sello (solo si se proporcionan llave y certificado)
        var sello = string.Empty;
        if (command.LlavePrivadaDer is not null && command.CertificadoDer is not null)
        {
            sello = _selloDigitalService.Firmar(cadenaOriginal, command.LlavePrivadaDer, command.PasswordLlave);
            var noCertificado = _selloDigitalService.ObtenerNoCertificado(command.CertificadoDer);
            var certificadoBase64 = _selloDigitalService.ObtenerCertificadoBase64(command.CertificadoDer);

            comprobante.AsignarSello(sello, certificadoBase64, noCertificado);
        }

        // 9. Serializar a string (XML final con sello/certificado/noCertificado)
        var xmlFinal = _cfdiSerializer.SerializarAString(comprobante);

        // 10. Retornar resultado
        return new CfdiResult(xmlFinal, cadenaOriginal, sello, comprobante.Id);
    }

    private static List<Concepto> BuildConceptos(IReadOnlyList<ConceptoDto> conceptoDtos, int decimalesMoneda)
    {
        var conceptos = new List<Concepto>(conceptoDtos.Count);

        foreach (var dto in conceptoDtos)
        {
            var valorUnitario = new MontoMoneda(dto.ValorUnitario, decimalesMoneda);
            var importe = new MontoMoneda(dto.Cantidad * dto.ValorUnitario, decimalesMoneda);

            MontoMoneda? descuento = dto.Descuento.HasValue
                ? new MontoMoneda(dto.Descuento.Value, decimalesMoneda)
                : null;

            var concepto = new Concepto(
                new ClaveCatalogo("c_ClaveProdServ", dto.ClaveProdServ),
                dto.Cantidad,
                new ClaveCatalogo("c_ClaveUnidad", dto.ClaveUnidad),
                dto.Descripcion,
                valorUnitario,
                importe,
                new ClaveCatalogo("c_ObjetoImp", dto.ObjetoImp),
                dto.NoIdentificacion,
                dto.Unidad,
                descuento);

            // Agregar traslados al concepto
            if (dto.Traslados is not null)
            {
                foreach (var trasladoDto in dto.Traslados)
                {
                    var baseTraslado = importe; // Base = Importe del concepto (antes de descuento según Anexo 20)

                    MontoMoneda? importeTraslado = null;
                    if (trasladoDto.TasaOCuota.HasValue)
                    {
                        importeTraslado = new MontoMoneda(
                            baseTraslado.Valor * trasladoDto.TasaOCuota.Value, decimalesMoneda);
                    }

                    var traslado = new TrasladoConcepto(
                        baseTraslado,
                        new ClaveCatalogo("c_Impuesto", trasladoDto.Impuesto),
                        new ClaveCatalogo("c_TipoFactor", trasladoDto.TipoFactor),
                        trasladoDto.TasaOCuota,
                        importeTraslado);

                    concepto.AgregarTraslado(traslado);
                }
            }

            conceptos.Add(concepto);
        }

        return conceptos;
    }
}

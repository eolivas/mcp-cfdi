using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Application.Commands.GenerarCfdi;
using McpCfdi.Application.DTOs;
using McpCfdi.Domain.Interfaces;
using Moq;
using Xunit;

namespace McpCfdi.Application.Tests;

/// <summary>
/// Property 10: Validación por lotes reporta todas las fallas
/// **Validates: Requirements 11.1, 11.2, 3.7, 4.10**
///
/// For any CFDI generation request containing N invalid catalog keys distributed across different fields,
/// the Validador_CFDI SHALL evaluate all keys without stopping at the first failure,
/// and the error response SHALL contain exactly N error entries, each identifying the field
/// and corresponding catalog.
/// </summary>
public class BatchValidationPropertyTests
{
    private const string CatalogErrorMarker = "no es válida en el catálogo";

    /// <summary>
    /// **Validates: Requirements 11.1, 11.2, 3.7, 4.10**
    /// For any N invalid catalog keys distributed across the command fields,
    /// ALL N failures should be reported (non-fail-fast behavior).
    /// </summary>
    [Fact]
    public void AllInvalidCatalogKeys_AreReported_WithoutFailFast()
    {
        var gen = GenInvalidKeyCount();
        var arb = gen.ToArbitrary();

        var prop = Prop.ForAll(arb, invalidKeyCount =>
        {
            // Build a command with the specified number of invalid catalog keys
            var (command, expectedInvalidKeys) = BuildCommandWithInvalidKeys(invalidKeyCount);

            // Setup mock catalog service to report failures for all INVALID keys
            var mockCatalogService = new Mock<ICatalogoSatService>();
            SetupMockCatalogService(mockCatalogService, expectedInvalidKeys);

            // Run the validator
            var validator = new GenerarCfdiCommandValidator(mockCatalogService.Object);
            var result = validator.ValidateAsync(command).GetAwaiter().GetResult();

            // Filter to only catalog-related errors
            var catalogErrors = result.Errors
                .Where(e => e.ErrorMessage.Contains(CatalogErrorMarker))
                .ToList();

            // The number of catalog errors must equal the number of invalid keys
            return (catalogErrors.Count == expectedInvalidKeys.Count).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Generator for invalid key count: between 1 and 13 (maximum catalog fields in a command
    /// with 1 concepto and 1 traslado: FormaPago, MetodoPago, Moneda, Exportacion,
    /// Emisor.RegimenFiscal, Receptor.RegimenFiscalReceptor, Receptor.UsoCfdi,
    /// Concepto.ClaveProdServ, Concepto.ClaveUnidad, Concepto.ObjetoImp,
    /// Traslado.Impuesto, Traslado.TipoFactor, Traslado.TasaOCuota).
    /// </summary>
    private static Gen<int> GenInvalidKeyCount()
    {
        return Gen.Choose(1, 13);
    }

    /// <summary>
    /// Builds a structurally valid command with the specified number of invalid catalog keys.
    /// Returns the command and the list of expected invalid key entries (clave, catalogo, campo).
    /// </summary>
    private static (GenerarCfdiCommand Command, List<(string Clave, string Catalogo, string Campo)> InvalidKeys)
        BuildCommandWithInvalidKeys(int invalidKeyCount)
    {
        // All possible catalog fields that can be invalid
        var allCatalogSlots = new List<(string Catalogo, string Campo, Func<string, ConceptoDto, TrasladoDto, (string? FormaPago, string? MetodoPago, string? Moneda, string? Exportacion, string? EmisorRegimen, string? ReceptorRegimen, string? UsoCfdi, ConceptoDto? Concepto, TrasladoDto? Traslado)> Setter)>();

        // We'll use a simpler approach: build command with fixed valid structure
        // and inject invalid keys into the catalog fields progressively.

        var invalidKeys = new List<(string Clave, string Catalogo, string Campo)>();

        // Define all catalog fields in order
        var catalogFields = new[]
        {
            ("c_FormaPago", "Comprobante.FormaPago"),
            ("c_MetodoPago", "Comprobante.MetodoPago"),
            ("c_Moneda", "Comprobante.Moneda"),
            ("c_Exportacion", "Comprobante.Exportacion"),
            ("c_RegimenFiscal", "Emisor.RegimenFiscal"),
            ("c_RegimenFiscal", "Receptor.RegimenFiscalReceptor"),
            ("c_UsoCFDI", "Receptor.UsoCfdi"),
            ("c_ClaveProdServ", "Conceptos[0].ClaveProdServ"),
            ("c_ClaveUnidad", "Conceptos[0].ClaveUnidad"),
            ("c_ObjetoImp", "Conceptos[0].ObjetoImp"),
            ("c_Impuesto", "Conceptos[0].Traslados[0].Impuesto"),
            ("c_TipoFactor", "Conceptos[0].Traslados[0].TipoFactor"),
            ("c_TasaOCuota", "Conceptos[0].Traslados[0].TasaOCuota"),
        };

        // Select the first N catalog fields to make invalid
        var fieldsToInvalidate = catalogFields.Take(invalidKeyCount).ToList();

        // Generate invalid keys
        for (var i = 0; i < fieldsToInvalidate.Count; i++)
        {
            var invalidKey = $"INVALID_{i + 1:D3}";
            invalidKeys.Add((invalidKey, fieldsToInvalidate[i].Item1, fieldsToInvalidate[i].Item2));
        }

        // Build the command with invalid keys in the selected slots
        var formaPago = GetKeyForField("Comprobante.FormaPago", fieldsToInvalidate, invalidKeys, "01");
        var metodoPago = GetKeyForField("Comprobante.MetodoPago", fieldsToInvalidate, invalidKeys, "PUE");
        var moneda = GetKeyForField("Comprobante.Moneda", fieldsToInvalidate, invalidKeys, "MXN");
        var exportacion = GetKeyForField("Comprobante.Exportacion", fieldsToInvalidate, invalidKeys, "01");
        var emisorRegimen = GetKeyForField("Emisor.RegimenFiscal", fieldsToInvalidate, invalidKeys, "601");
        var receptorRegimen = GetKeyForField("Receptor.RegimenFiscalReceptor", fieldsToInvalidate, invalidKeys, "601");
        var usoCfdi = GetKeyForField("Receptor.UsoCfdi", fieldsToInvalidate, invalidKeys, "G03");
        var claveProdServ = GetKeyForField("Conceptos[0].ClaveProdServ", fieldsToInvalidate, invalidKeys, "01010101");
        var claveUnidad = GetKeyForField("Conceptos[0].ClaveUnidad", fieldsToInvalidate, invalidKeys, "H87");
        var objetoImp = GetKeyForField("Conceptos[0].ObjetoImp", fieldsToInvalidate, invalidKeys, "02");
        var impuesto = GetKeyForField("Conceptos[0].Traslados[0].Impuesto", fieldsToInvalidate, invalidKeys, "002");
        var tipoFactor = GetKeyForField("Conceptos[0].Traslados[0].TipoFactor", fieldsToInvalidate, invalidKeys, "Tasa");
        var tasaOCuotaStr = GetKeyForField("Conceptos[0].Traslados[0].TasaOCuota", fieldsToInvalidate, invalidKeys, "0.160000");

        // Parse TasaOCuota - if it's an invalid key string, use a dummy decimal value
        // The validator sends TasaOCuota.Value.ToString("G") as the clave
        decimal? tasaOCuota = decimal.TryParse(tasaOCuotaStr, out var parsed) ? parsed : 0.999999m;

        // For TasaOCuota invalid key case, we need to ensure the decimal representation
        // matches what the validator will send. The validator sends traslado.TasaOCuota.Value.ToString("G")
        // We need to find the decimal value whose ToString("G") equals our invalid key.
        // Since we can't do that with "INVALID_013", we use a decimal whose ToString("G")
        // doesn't appear in valid catalogs - the mock will handle returning failure for it.
        if (fieldsToInvalidate.Any(f => f.Item2 == "Conceptos[0].Traslados[0].TasaOCuota"))
        {
            // Use a specific decimal value that the mock will identify as invalid
            tasaOCuota = 0.999999m;
            // Update the invalid key to match what the validator will actually send
            var idx = invalidKeys.FindIndex(k => k.Campo == "Conceptos[0].Traslados[0].TasaOCuota");
            if (idx >= 0)
            {
                invalidKeys[idx] = (tasaOCuota.Value.ToString("G"), "c_TasaOCuota", "Conceptos[0].Traslados[0].TasaOCuota");
            }
        }

        // TipoFactor for command: use the invalid key directly since validator sends the string as-is
        // But if TipoFactor is invalid AND TasaOCuota needs to be non-null,
        // we must handle the Tasa/Cuota logic: TasaOCuota is only sent when it HasValue
        var traslado = new TrasladoDto(impuesto, tipoFactor, tasaOCuota);

        var concepto = new ConceptoDto(
            ClaveProdServ: claveProdServ,
            Cantidad: 1m,
            ClaveUnidad: claveUnidad,
            Descripcion: "Test product",
            ValorUnitario: 100m,
            ObjetoImp: objetoImp,
            Traslados: new List<TrasladoDto> { traslado });

        var command = new GenerarCfdiCommand
        {
            Emisor = new EmisorDto("AAA010101AAA", "Emisor Test SA", emisorRegimen),
            Receptor = new ReceptorDto("XAXX010101000", "Público General", "12345", receptorRegimen, usoCfdi),
            Conceptos = new List<ConceptoDto> { concepto },
            FormaPago = formaPago,
            MetodoPago = metodoPago,
            Moneda = moneda,
            LugarExpedicion = "06600",
            Exportacion = exportacion,
            Fecha = new DateTime(2024, 1, 15)
        };

        return (command, invalidKeys);
    }

    /// <summary>
    /// Returns the invalid key if the field is in the invalidation list, otherwise the valid default.
    /// </summary>
    private static string GetKeyForField(
        string campo,
        List<(string Catalogo, string Campo)> fieldsToInvalidate,
        List<(string Clave, string Catalogo, string Campo)> invalidKeys,
        string validDefault)
    {
        var match = invalidKeys.FirstOrDefault(k => k.Campo == campo);
        return match != default ? match.Clave : validDefault;
    }

    /// <summary>
    /// Sets up the mock ICatalogoSatService to return failures for all keys present in the invalidKeys list.
    /// Valid keys pass validation. The mock also provides ObtenerDecimalesMonedaAsync for MXN.
    /// </summary>
    private static void SetupMockCatalogService(
        Mock<ICatalogoSatService> mock,
        List<(string Clave, string Catalogo, string Campo)> invalidKeys)
    {
        // Setup ValidarClavesAsync to return failures for invalid keys
        mock.Setup(s => s.ValidarClavesAsync(
                It.IsAny<IEnumerable<CatalogoValidationRequest>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns<IEnumerable<CatalogoValidationRequest>, DateTime, CancellationToken>((requests, fecha, ct) =>
            {
                var failures = new List<CatalogoValidationFailure>();

                foreach (var request in requests)
                {
                    // Check if this request matches any of our invalid keys
                    var isInvalid = invalidKeys.Any(ik =>
                        ik.Clave == request.Clave &&
                        ik.Catalogo == request.Catalogo &&
                        ik.Campo == request.CampoCfdi);

                    if (isInvalid)
                    {
                        failures.Add(new CatalogoValidationFailure(request.Clave, request.Catalogo, request.CampoCfdi));
                    }
                }

                return Task.FromResult(new CatalogoValidationResult(failures));
            });

        // Setup ObtenerDecimalesMonedaAsync - return 2 for MXN, throw for invalid moneda keys
        mock.Setup(s => s.ObtenerDecimalesMonedaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((moneda, ct) =>
            {
                // If it's a valid currency, return decimals; if invalid, throw to simulate unknown currency
                if (moneda == "MXN")
                    return Task.FromResult(2);
                // For invalid Moneda keys, throw so the validator skips Importe validation
                throw new KeyNotFoundException($"Moneda '{moneda}' not found");
            });
    }
}

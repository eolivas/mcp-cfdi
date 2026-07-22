using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using McpCfdi.Api.Mcp;
using McpCfdi.Application.Behaviours;
using McpCfdi.Application.Commands.GenerarCfdi;
using McpCfdi.Application.DTOs;
using McpCfdi.Domain.Interfaces;
using McpCfdi.Infrastructure.Xml;
using Moq;
using Xunit;

namespace McpCfdi.Api.Tests;

/// <summary>
/// Integration tests for the GenerarCfdiTool MCP tool end-to-end flow.
/// Tests the full pipeline: tool invocation → validation → domain → serialize → sign → response.
/// Validates: Requirements 1.1, 1.3, 10.5
/// </summary>
public class GenerarCfdiToolIntegrationTests
{
    private readonly Mock<ICatalogoSatService> _catalogoMock;
    private readonly Mock<ISelloDigitalService> _selloMock;
    private readonly ISender _mediator;
    private readonly GenerarCfdiTool _tool;

    public GenerarCfdiToolIntegrationTests()
    {
        _catalogoMock = new Mock<ICatalogoSatService>();
        _selloMock = new Mock<ISelloDigitalService>();

        // Configure catalog mock to return valid responses for standard keys
        _catalogoMock
            .Setup(x => x.ObtenerDecimalesMonedaAsync("MXN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _catalogoMock
            .Setup(x => x.ValidarClavesAsync(
                It.IsAny<IEnumerable<CatalogoValidationRequest>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogoValidationResult(Array.Empty<CatalogoValidationFailure>()));

        // Wire up real MediatR pipeline with validation behaviour
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GenerarCfdiCommandHandler).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });
        services.AddValidatorsFromAssembly(typeof(GenerarCfdiCommandValidator).Assembly);

        // Register real infrastructure services
        services.AddScoped<ICfdiSerializer, CfdiXmlSerializer>();
        services.AddScoped<ICadenaOriginalGenerator, XsltCadenaOriginalGenerator>();

        // Register mocks
        services.AddScoped(_ => _catalogoMock.Object);
        services.AddScoped(_ => _selloMock.Object);

        var provider = services.BuildServiceProvider();
        _mediator = provider.GetRequiredService<ISender>();
        _tool = new GenerarCfdiTool();
    }

    [Fact]
    public async Task GenerarAsync_ValidCfdiData_ReturnsSuccessWithXml()
    {
        // Arrange
        var emisor = new EmisorDto("AAA010101AAA", "Empresa de Prueba SA de CV", "601");
        var receptor = new ReceptorDto(
            "XAXX010101000",
            "Público General",
            "06600",
            "616",
            "S01");

        var traslado = new TrasladoDto("002", "Tasa", 0.160000m);
        var concepto = new ConceptoDto(
            ClaveProdServ: "01010101",
            Cantidad: 2m,
            ClaveUnidad: "H87",
            Descripcion: "Servicio de consultoría",
            ValorUnitario: 1500.00m,
            ObjetoImp: "02",
            Traslados: new List<TrasladoDto> { traslado });

        // Act
        var response = await _tool.GenerarAsync(
            _mediator,
            emisor,
            receptor,
            new List<ConceptoDto> { concepto },
            FormaPago: "01",
            MetodoPago: "PUE",
            Moneda: "MXN",
            LugarExpedicion: "06600",
            Exportacion: "01",
            ct: CancellationToken.None,
            Fecha: new DateTime(2024, 3, 15, 10, 30, 0));

        // Assert
        response.IsError.Should().NotBe(true, "the response should not be an error for valid CFDI data");
        response.Content.Should().NotBeEmpty();

        var xmlText = response.Content[0].Text;
        xmlText.Should().NotBeNullOrEmpty("the response content must contain XML");
        xmlText.Should().Contain("http://www.sat.gob.mx/cfd/4", "the XML must use the CFDI 4.0 namespace");
        xmlText.Should().Contain("Emisor", "the XML must contain an Emisor node");
        xmlText.Should().Contain("Receptor", "the XML must contain a Receptor node");
        xmlText.Should().Contain("Conceptos", "the XML must contain a Conceptos node");
    }

    [Fact]
    public async Task GenerarAsync_InvalidRfcFormat_ReturnsValidationError()
    {
        // Arrange - use an invalid RFC format (too short, invalid structure)
        var emisor = new EmisorDto("INVALID", "Empresa Inválida", "601");
        var receptor = new ReceptorDto(
            "XAXX010101000",
            "Público General",
            "06600",
            "616",
            "S01");

        var concepto = new ConceptoDto(
            ClaveProdServ: "01010101",
            Cantidad: 1m,
            ClaveUnidad: "H87",
            Descripcion: "Servicio de prueba",
            ValorUnitario: 1000.00m,
            ObjetoImp: "02",
            Traslados: new List<TrasladoDto> { new("002", "Tasa", 0.160000m) });

        // Act
        var response = await _tool.GenerarAsync(
            _mediator,
            emisor,
            receptor,
            new List<ConceptoDto> { concepto },
            FormaPago: "01",
            MetodoPago: "PUE",
            Moneda: "MXN",
            LugarExpedicion: "06600",
            Exportacion: "01",
            ct: CancellationToken.None,
            Fecha: new DateTime(2024, 1, 15, 10, 0, 0));

        // Assert
        response.IsError.Should().BeTrue("the response should be an error for invalid RFC");
        response.Content.Should().NotBeEmpty();

        var errorText = response.Content[0].Text;
        errorText.Should().StartWith("Error de validación al generar CFDI:\n- ",
            "the error format must match the MCP error structure");
        errorText.Should().Contain("RFC", "the error should mention the invalid RFC field");
    }

    [Fact]
    public async Task GenerarAsync_WithSigningData_ReturnsXmlWithSelloAttributes()
    {
        // Arrange
        const string expectedSello = "YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXo=";
        const string expectedNoCertificado = "30001000000400002434";
        const string expectedCertificadoBase64 = "MIIF+TCCA+GgAwIBAgIU...base64cert...==";

        _selloMock
            .Setup(x => x.Firmar(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string?>()))
            .Returns(expectedSello);

        _selloMock
            .Setup(x => x.ObtenerNoCertificado(It.IsAny<byte[]>()))
            .Returns(expectedNoCertificado);

        _selloMock
            .Setup(x => x.ObtenerCertificadoBase64(It.IsAny<byte[]>()))
            .Returns(expectedCertificadoBase64);

        var emisor = new EmisorDto("AAA010101AAA", "Empresa Firmada SA de CV", "601");
        var receptor = new ReceptorDto(
            "XAXX010101000",
            "Público General",
            "06600",
            "616",
            "S01");

        var traslado = new TrasladoDto("002", "Tasa", 0.160000m);
        var concepto = new ConceptoDto(
            ClaveProdServ: "01010101",
            Cantidad: 1m,
            ClaveUnidad: "H87",
            Descripcion: "Servicio con firma digital",
            ValorUnitario: 5000.00m,
            ObjetoImp: "02",
            Traslados: new List<TrasladoDto> { traslado });

        var llavePrivadaDer = new byte[] { 0x30, 0x82, 0x01, 0x22 }; // Dummy DER bytes
        var certificadoDer = new byte[] { 0x30, 0x82, 0x03, 0x5A }; // Dummy cert bytes

        // Act
        var response = await _tool.GenerarAsync(
            _mediator,
            emisor,
            receptor,
            new List<ConceptoDto> { concepto },
            FormaPago: "03",
            MetodoPago: "PUE",
            Moneda: "MXN",
            LugarExpedicion: "06600",
            Exportacion: "01",
            ct: CancellationToken.None,
            Fecha: new DateTime(2024, 6, 1, 14, 0, 0),
            LlavePrivadaDer: llavePrivadaDer,
            PasswordLlave: "12345678a",
            CertificadoDer: certificadoDer);

        // Assert
        response.IsError.Should().NotBe(true, "the response should not be an error for valid signed CFDI");
        response.Content.Should().NotBeEmpty();

        var xmlText = response.Content[0].Text;
        xmlText.Should().NotBeNullOrEmpty();

        // Verify sello-related attributes are present in the XML
        xmlText.Should().Contain($"NoCertificado=\"{expectedNoCertificado}\"",
            "the XML must contain the NoCertificado attribute");
        xmlText.Should().Contain($"Certificado=\"{expectedCertificadoBase64}\"",
            "the XML must contain the Certificado attribute");
        xmlText.Should().Contain($"Sello=\"{expectedSello}\"",
            "the XML must contain the Sello attribute");

        // Verify the cadena original was generated (sello service was called)
        _selloMock.Verify(
            x => x.Firmar(It.Is<string>(s => s.StartsWith("||")), llavePrivadaDer, "12345678a"),
            Times.Once,
            "the cadena original must start with '||' and be passed to the signing service");
    }
}

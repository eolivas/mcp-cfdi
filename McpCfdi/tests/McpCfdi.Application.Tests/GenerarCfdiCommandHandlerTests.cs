using System.Xml.Linq;
using FluentAssertions;
using McpCfdi.Application.Commands.GenerarCfdi;
using McpCfdi.Application.DTOs;
using McpCfdi.Domain.Entities;
using McpCfdi.Domain.Interfaces;
using McpCfdi.Infrastructure.Exceptions;
using Moq;
using Xunit;

namespace McpCfdi.Application.Tests;

public class GenerarCfdiCommandHandlerTests
{
    private readonly Mock<ICatalogoSatService> _catalogoSatServiceMock;
    private readonly Mock<ICfdiSerializer> _cfdiSerializerMock;
    private readonly Mock<ICadenaOriginalGenerator> _cadenaOriginalGeneratorMock;
    private readonly Mock<ISelloDigitalService> _selloDigitalServiceMock;
    private readonly GenerarCfdiCommandHandler _handler;

    public GenerarCfdiCommandHandlerTests()
    {
        _catalogoSatServiceMock = new Mock<ICatalogoSatService>();
        _cfdiSerializerMock = new Mock<ICfdiSerializer>();
        _cadenaOriginalGeneratorMock = new Mock<ICadenaOriginalGenerator>();
        _selloDigitalServiceMock = new Mock<ISelloDigitalService>();

        _handler = new GenerarCfdiCommandHandler(
            _catalogoSatServiceMock.Object,
            _cfdiSerializerMock.Object,
            _cadenaOriginalGeneratorMock.Object,
            _selloDigitalServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCfdiResultWithNonEmptyFields()
    {
        // Arrange
        var command = CreateValidCommand(includeSigning: true);

        _catalogoSatServiceMock
            .Setup(x => x.ObtenerDecimalesMonedaAsync("MXN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _cfdiSerializerMock
            .Setup(x => x.Serializar(It.IsAny<Comprobante>()))
            .Returns(new XDocument(new XElement("cfdi")));

        _cfdiSerializerMock
            .Setup(x => x.SerializarAString(It.IsAny<Comprobante>()))
            .Returns("<cfdi:Comprobante />");

        _cadenaOriginalGeneratorMock
            .Setup(x => x.Generar(It.IsAny<XDocument>()))
            .Returns("||4.0|A|...||");

        _selloDigitalServiceMock
            .Setup(x => x.Firmar(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string?>()))
            .Returns("c2VsbG9CYXNlNjQ=");

        _selloDigitalServiceMock
            .Setup(x => x.ObtenerNoCertificado(It.IsAny<byte[]>()))
            .Returns("12345678901234567890");

        _selloDigitalServiceMock
            .Setup(x => x.ObtenerCertificadoBase64(It.IsAny<byte[]>()))
            .Returns("Y2VydGlmaWNhZG9CYXNlNjQ=");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Xml.Should().NotBeNullOrEmpty();
        result.CadenaOriginal.Should().NotBeNullOrEmpty();
        result.Sello.Should().NotBeNullOrEmpty();
        result.ComprobanteId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithoutSigning_ReturnsCfdiResultWithEmptySello()
    {
        // Arrange
        var command = CreateValidCommand(includeSigning: false);

        _catalogoSatServiceMock
            .Setup(x => x.ObtenerDecimalesMonedaAsync("MXN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _cfdiSerializerMock
            .Setup(x => x.Serializar(It.IsAny<Comprobante>()))
            .Returns(new XDocument(new XElement("cfdi")));

        _cfdiSerializerMock
            .Setup(x => x.SerializarAString(It.IsAny<Comprobante>()))
            .Returns("<cfdi:Comprobante />");

        _cadenaOriginalGeneratorMock
            .Setup(x => x.Generar(It.IsAny<XDocument>()))
            .Returns("||4.0|A|...||");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Xml.Should().NotBeNullOrEmpty();
        result.Sello.Should().BeEmpty();
        _selloDigitalServiceMock.Verify(
            x => x.Firmar(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CatalogUnavailable_PropagatesCatalogoUnavailableException()
    {
        // Arrange
        var command = CreateValidCommand(includeSigning: true);

        _catalogoSatServiceMock
            .Setup(x => x.ObtenerDecimalesMonedaAsync("MXN", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CatalogoUnavailableException(
                "c_Moneda",
                "No se pudo obtener los decimales de la moneda 'MXN' porque la base de datos no está disponible."));

        // Act & Assert
        await Assert.ThrowsAsync<CatalogoUnavailableException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    private static GenerarCfdiCommand CreateValidCommand(bool includeSigning)
    {
        var emisor = new EmisorDto("AAA010101AAA", "Test Emisor", "601");
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
            Descripcion: "Servicio de prueba",
            ValorUnitario: 1000.00m,
            ObjetoImp: "02",
            Traslados: new List<TrasladoDto> { traslado });

        return new GenerarCfdiCommand
        {
            Emisor = emisor,
            Receptor = receptor,
            Conceptos = new List<ConceptoDto> { concepto },
            FormaPago = "01",
            MetodoPago = "PUE",
            Moneda = "MXN",
            LugarExpedicion = "06600",
            Exportacion = "01",
            Fecha = new DateTime(2024, 1, 15, 10, 30, 0),
            LlavePrivadaDer = includeSigning ? new byte[] { 0x01, 0x02, 0x03 } : null,
            PasswordLlave = includeSigning ? "12345678a" : null,
            CertificadoDer = includeSigning ? new byte[] { 0x04, 0x05, 0x06 } : null
        };
    }
}

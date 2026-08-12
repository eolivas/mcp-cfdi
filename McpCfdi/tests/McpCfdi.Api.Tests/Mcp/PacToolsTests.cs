using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using McpCfdi.Api.Mcp;
using McpCfdi.Application.Commands.CancelarCfdi;
using McpCfdi.Application.Commands.TimbrarCfdi;
using McpCfdi.Application.DTOs;
using McpCfdi.Application.Queries.ConsultarEstatusCfdi;
using McpCfdi.Infrastructure.Exceptions;
using Moq;
using Xunit;

namespace McpCfdi.Api.Tests.Mcp;

/// <summary>
/// Unit tests for MCP PAC tools (Timbrar, Cancelar, ConsultarEstatus).
/// Verifies each tool correctly delegates to MediatR ISender and maps results/errors.
/// Validates: Requirements 7.1, 7.2, 7.3
/// </summary>
public class PacToolsTests
{
    private readonly Mock<ISender> _mediatorMock;

    public PacToolsTests()
    {
        _mediatorMock = new Mock<ISender>();
    }

    #region TimbrarCfdiTool

    [Fact]
    public async Task TimbrarAsync_DelegatesCorrectly_ReturnsCfdiXml()
    {
        // Arrange
        var expectedXml = "<cfdi:Comprobante>...timbrado...</cfdi:Comprobante>";
        var timbradoResult = new TimbradoResult(
            Uuid: "12345678-ABCD-1234-EFGH-123456789012",
            FechaTimbrado: new DateTime(2024, 6, 15, 10, 30, 0),
            SelloSat: "sello-sat-base64",
            NoCertificadoSat: "00001000000504465028",
            SelloCfd: "sello-cfd-base64",
            CfdiTimbradoXml: expectedXml);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<TimbrarCfdiCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(timbradoResult);

        var tool = new TimbrarCfdiTool();
        var xmlSellado = "<cfdi:Comprobante>...sellado...</cfdi:Comprobante>";

        // Act
        var result = await tool.TimbrarAsync(_mediatorMock.Object, xmlSellado, CancellationToken.None);

        // Assert
        result.IsError.Should().NotBe(true);
        result.Content.Should().NotBeEmpty();
        result.Content[0].Text.Should().Be(expectedXml);

        _mediatorMock.Verify(m => m.Send(
            It.Is<TimbrarCfdiCommand>(cmd => cmd.CfdiXmlSellado == xmlSellado),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TimbrarAsync_WhenValidationException_ReturnsErrorResponse()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("CfdiXmlSellado", "El XML sellado es requerido")
        };
        var validationException = new ValidationException(failures);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<TimbrarCfdiCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        var tool = new TimbrarCfdiTool();

        // Act
        var result = await tool.TimbrarAsync(_mediatorMock.Object, "", CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content.Should().NotBeEmpty();
        result.Content[0].Text.Should().Contain("validación");
    }

    [Fact]
    public async Task TimbrarAsync_WhenPacValidationException_ReturnsErrorResponse()
    {
        // Arrange
        var pacException = new PacValidationException(
            "CFDI no cumple reglas SAT",
            pacProvider: "Multifacturas",
            codigoError: "301",
            detalleError: "Sello del emisor inválido");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<TimbrarCfdiCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(pacException);

        var tool = new TimbrarCfdiTool();

        // Act
        var result = await tool.TimbrarAsync(_mediatorMock.Object, "<xml/>", CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content.Should().NotBeEmpty();
        result.Content[0].Text.Should().Contain("301");
        result.Content[0].Text.Should().Contain("Sello del emisor inválido");
    }

    #endregion

    #region CancelarCfdiTool

    [Fact]
    public async Task CancelarAsync_DelegatesCorrectly_ReturnsFormattedResult()
    {
        // Arrange
        var uuid = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE";
        var rfcEmisor = "AAA010101AAA";
        var motivo = "02";
        var uuidSustitucion = "11111111-2222-3333-4444-555555555555";

        var cancelacionResult = new CancelacionResult(
            Uuid: uuid,
            EstatusUuid: "Cancelado",
            AcuseXml: "<Acuse>...</Acuse>",
            FechaCancelacion: new DateTime(2024, 6, 15, 12, 0, 0));

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CancelarCfdiCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelacionResult);

        var tool = new CancelarCfdiTool();

        // Act
        var result = await tool.CancelarAsync(
            _mediatorMock.Object, uuid, rfcEmisor, motivo, CancellationToken.None, uuidSustitucion);

        // Assert
        result.IsError.Should().NotBe(true);
        result.Content.Should().NotBeEmpty();
        result.Content[0].Text.Should().Contain("Cancelación exitosa");
        result.Content[0].Text.Should().Contain(uuid);
        result.Content[0].Text.Should().Contain("Cancelado");

        _mediatorMock.Verify(m => m.Send(
            It.Is<CancelarCfdiCommand>(cmd =>
                cmd.Uuid == uuid &&
                cmd.RfcEmisor == rfcEmisor &&
                cmd.Motivo == motivo &&
                cmd.UuidSustitucion == uuidSustitucion),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarAsync_WithNullUuidSustitucion_DelegatesCorrectly()
    {
        // Arrange
        var uuid = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE";
        var rfcEmisor = "AAA010101AAA";
        var motivo = "03";

        var cancelacionResult = new CancelacionResult(
            Uuid: uuid,
            EstatusUuid: "Cancelado",
            AcuseXml: "<Acuse>...</Acuse>",
            FechaCancelacion: new DateTime(2024, 6, 15, 12, 0, 0));

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CancelarCfdiCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelacionResult);

        var tool = new CancelarCfdiTool();

        // Act
        var result = await tool.CancelarAsync(
            _mediatorMock.Object, uuid, rfcEmisor, motivo, CancellationToken.None);

        // Assert
        result.IsError.Should().NotBe(true);

        _mediatorMock.Verify(m => m.Send(
            It.Is<CancelarCfdiCommand>(cmd =>
                cmd.Uuid == uuid &&
                cmd.RfcEmisor == rfcEmisor &&
                cmd.Motivo == motivo &&
                cmd.UuidSustitucion == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarAsync_WhenPacException_ReturnsErrorResponse()
    {
        // Arrange
        var pacException = new PacIntegrationException(
            "Error inesperado al cancelar", pacProvider: "Multifacturas");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CancelarCfdiCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(pacException);

        var tool = new CancelarCfdiTool();

        // Act
        var result = await tool.CancelarAsync(
            _mediatorMock.Object, "uuid", "rfc", "02", CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content.Should().NotBeEmpty();
        result.Content[0].Text.Should().Contain("integración");
    }

    #endregion

    #region ConsultarEstatusCfdiTool

    [Fact]
    public async Task ConsultarAsync_DelegatesCorrectly_ReturnsFormattedResult()
    {
        // Arrange
        var rfcEmisor = "AAA010101AAA";
        var rfcReceptor = "XAXX010101000";
        var total = "1500.00";
        var uuid = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE";

        var estatusResult = new EstatusCfdiResult(
            Estado: "Vigente",
            EstatusCancelacion: "No cancelable",
            EsCancelable: false);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ConsultarEstatusCfdiQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(estatusResult);

        var tool = new ConsultarEstatusCfdiTool();

        // Act
        var result = await tool.ConsultarAsync(
            _mediatorMock.Object, rfcEmisor, rfcReceptor, total, uuid, CancellationToken.None);

        // Assert
        result.IsError.Should().NotBe(true);
        result.Content.Should().NotBeEmpty();
        result.Content[0].Text.Should().Contain("Vigente");
        result.Content[0].Text.Should().Contain("No");
        result.Content[0].Text.Should().Contain(uuid);

        _mediatorMock.Verify(m => m.Send(
            It.Is<ConsultarEstatusCfdiQuery>(q =>
                q.RfcEmisor == rfcEmisor &&
                q.RfcReceptor == rfcReceptor &&
                q.Total == total &&
                q.Uuid == uuid),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConsultarAsync_WhenPacException_ReturnsErrorResponse()
    {
        // Arrange
        var pacException = new PacTransientException(
            "Timeout al consultar SAT", pacProvider: "Multifacturas");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ConsultarEstatusCfdiQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(pacException);

        var tool = new ConsultarEstatusCfdiTool();

        // Act
        var result = await tool.ConsultarAsync(
            _mediatorMock.Object, "rfc1", "rfc2", "100.00", "uuid", CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content.Should().NotBeEmpty();
        result.Content[0].Text.Should().Contain("transitorio");
    }

    #endregion
}

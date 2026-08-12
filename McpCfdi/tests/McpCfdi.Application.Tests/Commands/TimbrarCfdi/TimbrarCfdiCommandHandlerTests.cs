using FluentAssertions;
using McpCfdi.Application.Commands.TimbrarCfdi;
using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using NSubstitute;
using Xunit;

namespace McpCfdi.Application.Tests.Commands.TimbrarCfdi;

public class TimbrarCfdiCommandHandlerTests
{
    private readonly IPacService _pacService;
    private readonly TimbrarCfdiCommandHandler _sut;

    public TimbrarCfdiCommandHandlerTests()
    {
        _pacService = Substitute.For<IPacService>();
        _sut = new TimbrarCfdiCommandHandler(_pacService);
    }

    [Fact]
    public async Task Handle_CallsTimbrarAsyncWithCfdiXmlSellado()
    {
        // Arrange
        var command = new TimbrarCfdiCommand { CfdiXmlSellado = "<cfdi:Comprobante />" };
        var expectedResult = new TimbradoResult(
            Uuid: "AAA-BBB-CCC",
            FechaTimbrado: new DateTime(2024, 1, 15, 10, 30, 0),
            SelloSat: "selloSat123",
            NoCertificadoSat: "00001000000506258094",
            SelloCfd: "selloCfd456",
            CfdiTimbradoXml: "<cfdi:Comprobante Timbrado />");

        _pacService.TimbrarAsync(command.CfdiXmlSellado, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _pacService.Received(1).TimbrarAsync(command.CfdiXmlSellado, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsTimbradoResultFromPacService()
    {
        // Arrange
        var command = new TimbrarCfdiCommand { CfdiXmlSellado = "<cfdi:Comprobante />" };
        var expectedResult = new TimbradoResult(
            Uuid: "12345678-ABCD-EFGH-IJKL-123456789012",
            FechaTimbrado: new DateTime(2024, 6, 20, 14, 0, 0),
            SelloSat: "selloSatXYZ",
            NoCertificadoSat: "00001000000506258094",
            SelloCfd: "selloCfdXYZ",
            CfdiTimbradoXml: "<tfd:TimbreFiscalDigital />");

        _pacService.TimbrarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }
}

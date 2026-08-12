using FluentAssertions;
using McpCfdi.Application.Commands.CancelarCfdi;
using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using NSubstitute;
using Xunit;

namespace McpCfdi.Application.Tests.Commands.CancelarCfdi;

public class CancelarCfdiCommandHandlerTests
{
    private readonly IPacService _pacService;
    private readonly IEmisorCredencialesProvider _credencialesProvider;
    private readonly CancelarCfdiCommandHandler _sut;

    public CancelarCfdiCommandHandlerTests()
    {
        _pacService = Substitute.For<IPacService>();
        _credencialesProvider = Substitute.For<IEmisorCredencialesProvider>();
        _sut = new CancelarCfdiCommandHandler(_pacService, _credencialesProvider);
    }

    [Fact]
    public async Task Handle_CallsObtenerCredencialesAsyncWithRfcEmisor()
    {
        // Arrange
        var command = new CancelarCfdiCommand
        {
            Uuid = "AAA-BBB-CCC",
            RfcEmisor = "EKU9003173C9",
            Motivo = "02",
            UuidSustitucion = null
        };

        var credenciales = new EmisorCredenciales(
            Rfc: "EKU9003173C9",
            CertificadoDer: new byte[] { 1, 2, 3 },
            LlavePrivadaDer: new byte[] { 4, 5, 6 },
            PasswordLlave: "12345678a");

        _credencialesProvider.ObtenerCredencialesAsync(command.RfcEmisor, Arg.Any<CancellationToken>())
            .Returns(credenciales);

        _pacService.CancelarAsync(Arg.Any<CancelacionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CancelacionResult("AAA-BBB-CCC", "Cancelado", "<acuse/>", DateTime.UtcNow));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _credencialesProvider.Received(1)
            .ObtenerCredencialesAsync(command.RfcEmisor, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConstructsCancelacionRequestWithCorrectFields()
    {
        // Arrange
        var certBytes = new byte[] { 10, 20, 30, 40 };
        var keyBytes = new byte[] { 50, 60, 70, 80 };

        var command = new CancelarCfdiCommand
        {
            Uuid = "12345678-ABCD-EFGH-IJKL-123456789012",
            RfcEmisor = "EKU9003173C9",
            Motivo = "01",
            UuidSustitucion = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE"
        };

        var credenciales = new EmisorCredenciales(
            Rfc: "EKU9003173C9",
            CertificadoDer: certBytes,
            LlavePrivadaDer: keyBytes,
            PasswordLlave: "miPassword123");

        _credencialesProvider.ObtenerCredencialesAsync(command.RfcEmisor, Arg.Any<CancellationToken>())
            .Returns(credenciales);

        CancelacionRequest? capturedRequest = null;
        _pacService.CancelarAsync(Arg.Do<CancelacionRequest>(r => capturedRequest = r), Arg.Any<CancellationToken>())
            .Returns(new CancelacionResult(command.Uuid, "Cancelado", "<acuse/>", DateTime.UtcNow));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Uuid.Should().Be(command.Uuid);
        capturedRequest.RfcEmisor.Should().Be(command.RfcEmisor);
        capturedRequest.Motivo.Should().Be(command.Motivo);
        capturedRequest.UuidSustitucion.Should().Be(command.UuidSustitucion);
        capturedRequest.CertificadoBase64.Should().Be(Convert.ToBase64String(certBytes));
        capturedRequest.LlavePrivadaBase64.Should().Be(Convert.ToBase64String(keyBytes));
        capturedRequest.PasswordLlave.Should().Be("miPassword123");
    }

    [Fact]
    public async Task Handle_CallsCancelarAsyncWithConstructedRequest()
    {
        // Arrange
        var command = new CancelarCfdiCommand
        {
            Uuid = "AAA-BBB-CCC",
            RfcEmisor = "EKU9003173C9",
            Motivo = "02",
            UuidSustitucion = null
        };

        var credenciales = new EmisorCredenciales(
            Rfc: "EKU9003173C9",
            CertificadoDer: new byte[] { 1 },
            LlavePrivadaDer: new byte[] { 2 },
            PasswordLlave: "pwd");

        _credencialesProvider.ObtenerCredencialesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(credenciales);

        var expectedResult = new CancelacionResult("AAA-BBB-CCC", "Cancelado", "<acuse/>", new DateTime(2024, 7, 1));
        _pacService.CancelarAsync(Arg.Any<CancelacionRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _pacService.Received(1).CancelarAsync(Arg.Any<CancelacionRequest>(), Arg.Any<CancellationToken>());
        result.Should().Be(expectedResult);
    }
}

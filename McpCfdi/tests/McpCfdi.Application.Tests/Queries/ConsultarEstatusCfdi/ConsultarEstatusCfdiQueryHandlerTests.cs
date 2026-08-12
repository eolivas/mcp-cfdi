using FluentAssertions;
using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using McpCfdi.Application.Queries.ConsultarEstatusCfdi;
using NSubstitute;
using Xunit;

namespace McpCfdi.Application.Tests.Queries.ConsultarEstatusCfdi;

public class ConsultarEstatusCfdiQueryHandlerTests
{
    private readonly IPacService _pacService;
    private readonly ConsultarEstatusCfdiQueryHandler _sut;

    public ConsultarEstatusCfdiQueryHandlerTests()
    {
        _pacService = Substitute.For<IPacService>();
        _sut = new ConsultarEstatusCfdiQueryHandler(_pacService);
    }

    [Fact]
    public async Task Handle_ConstructsConsultaEstatusRequestWithCorrectFields()
    {
        // Arrange
        var query = new ConsultarEstatusCfdiQuery
        {
            RfcEmisor = "EKU9003173C9",
            RfcReceptor = "XAXX010101000",
            Total = "1500.00",
            Uuid = "12345678-ABCD-EFGH-IJKL-123456789012"
        };

        ConsultaEstatusRequest? capturedRequest = null;
        _pacService.ConsultarEstatusAsync(Arg.Do<ConsultaEstatusRequest>(r => capturedRequest = r), Arg.Any<CancellationToken>())
            .Returns(new EstatusCfdiResult("Vigente", "No cancelable", false));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RfcEmisor.Should().Be(query.RfcEmisor);
        capturedRequest.RfcReceptor.Should().Be(query.RfcReceptor);
        capturedRequest.Total.Should().Be(query.Total);
        capturedRequest.Uuid.Should().Be(query.Uuid);
    }

    [Fact]
    public async Task Handle_CallsConsultarEstatusAsyncWithConstructedRequest()
    {
        // Arrange
        var query = new ConsultarEstatusCfdiQuery
        {
            RfcEmisor = "EKU9003173C9",
            RfcReceptor = "XAXX010101000",
            Total = "2000.00",
            Uuid = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE"
        };

        _pacService.ConsultarEstatusAsync(Arg.Any<ConsultaEstatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EstatusCfdiResult("Vigente", "", true));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _pacService.Received(1)
            .ConsultarEstatusAsync(Arg.Any<ConsultaEstatusRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsEstatusCfdiResultFromPacService()
    {
        // Arrange
        var query = new ConsultarEstatusCfdiQuery
        {
            RfcEmisor = "EKU9003173C9",
            RfcReceptor = "XAXX010101000",
            Total = "3500.50",
            Uuid = "11111111-2222-3333-4444-555555555555"
        };

        var expectedResult = new EstatusCfdiResult("Cancelado", "Cancelado sin aceptación", true);
        _pacService.ConsultarEstatusAsync(Arg.Any<ConsultaEstatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }
}

using System.Net;
using McpCfdi.Application.DTOs;
using McpCfdi.Infrastructure.Exceptions;
using McpCfdi.Infrastructure.Pac;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Pac;

/// <summary>
/// Integration tests for MultifacturasPacAdapter using WireMock.Net
/// to simulate the Multifacturas PAC API.
/// Validates: Requirements 5.1, 5.3
/// </summary>
public class MultifacturasPacAdapterIntegrationTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly MultifacturasPacAdapter _adapter;

    public MultifacturasPacAdapterIntegrationTests()
    {
        _server = WireMockServer.Start();

        var httpClient = new HttpClient { BaseAddress = new Uri(_server.Url!) };

        var pacOptions = Options.Create(new PacOptions
        {
            ActiveProvider = "Multifacturas",
            Multifacturas = new MultifacturasPacOptions
            {
                BaseUrl = _server.Url!,
                ApiKey = "test-api-key",
                Usuario = "test-user",
                Password = "test-pass"
            }
        });

        var logger = NullLogger<MultifacturasPacAdapter>.Instance;

        _adapter = new MultifacturasPacAdapter(httpClient, pacOptions, logger);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    [Fact]
    public async Task TimbrarAsync_ExitosoRetornaUuidYXml()
    {
        // Arrange
        var expectedUuid = "6F4A20E7-9B4D-4C8A-A25F-3E7D6C9B8A1F";
        var expectedXml = "<cfdi:Comprobante><tfd:TimbreFiscalDigital /></cfdi:Comprobante>";

        _server
            .Given(Request.Create().WithPath("/api/stamp").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                {
                    "uuid": "{{expectedUuid}}",
                    "fechaTimbrado": "2024-01-15T10:30:00",
                    "selloSat": "ABC123SelloSat",
                    "noCertificadoSat": "00001000000504465028",
                    "selloCfd": "XYZ789SelloCfd",
                    "cfdiTimbradoXml": "{{expectedXml}}"
                }
                """));

        // Act
        var result = await _adapter.TimbrarAsync("<cfdi:Comprobante>test</cfdi:Comprobante>");

        // Assert
        Assert.Equal(expectedUuid, result.Uuid);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0), result.FechaTimbrado);
        Assert.Equal("ABC123SelloSat", result.SelloSat);
        Assert.Equal("00001000000504465028", result.NoCertificadoSat);
        Assert.Equal("XYZ789SelloCfd", result.SelloCfd);
        Assert.Equal(expectedXml, result.CfdiTimbradoXml);
    }

    [Fact]
    public async Task TimbrarAsync_Error500LanzaPacTransientException()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/api/stamp").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PacTransientException>(
            () => _adapter.TimbrarAsync("<cfdi:Comprobante>test</cfdi:Comprobante>"));

        Assert.Contains("500", ex.Message);
        Assert.Equal("Multifacturas", ex.PacProvider);
    }

    [Fact]
    public async Task TimbrarAsync_Error400LanzaPacValidationExceptionConCodigoYDetalle()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/api/stamp").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "codigo": "CFDI33001",
                    "detalle": "El sello es inválido"
                }
                """));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PacValidationException>(
            () => _adapter.TimbrarAsync("<cfdi:Comprobante>test</cfdi:Comprobante>"));

        Assert.Equal("CFDI33001", ex.CodigoError);
        Assert.Equal("El sello es inválido", ex.DetalleError);
        Assert.Equal("Multifacturas", ex.PacProvider);
    }

    [Fact]
    public async Task TimbrarAsync_Error401LanzaPacAuthenticationException()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/api/stamp").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("Unauthorized"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PacAuthenticationException>(
            () => _adapter.TimbrarAsync("<cfdi:Comprobante>test</cfdi:Comprobante>"));

        Assert.Equal("Multifacturas", ex.PacProvider);
    }

    [Fact]
    public async Task TimbrarAsync_Error402LanzaPacInsufficientCreditsException()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/api/stamp").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(402)
                .WithBody("Payment Required"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PacInsufficientCreditsException>(
            () => _adapter.TimbrarAsync("<cfdi:Comprobante>test</cfdi:Comprobante>"));

        Assert.Equal("Multifacturas", ex.PacProvider);
    }

    [Fact]
    public async Task CancelarAsync_ExitosoRetornaAcuseYEstatus()
    {
        // Arrange
        var expectedUuid = "6F4A20E7-9B4D-4C8A-A25F-3E7D6C9B8A1F";

        _server
            .Given(Request.Create().WithPath("/api/cancel").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                {
                    "uuid": "{{expectedUuid}}",
                    "estatusUuid": "201 - UUID Cancelado",
                    "acuseXml": "<Acuse>contenido</Acuse>",
                    "fechaCancelacion": "2024-01-15T10:30:00"
                }
                """));

        var request = new CancelacionRequest(
            Uuid: expectedUuid,
            RfcEmisor: "XAXX010101000",
            Motivo: "02",
            UuidSustitucion: null,
            CertificadoBase64: "base64cert",
            LlavePrivadaBase64: "base64key",
            PasswordLlave: "12345678a");

        // Act
        var result = await _adapter.CancelarAsync(request);

        // Assert
        Assert.Equal(expectedUuid, result.Uuid);
        Assert.Equal("201 - UUID Cancelado", result.EstatusUuid);
        Assert.Equal("<Acuse>contenido</Acuse>", result.AcuseXml);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0), result.FechaCancelacion);
    }

    [Fact]
    public async Task ConsultarEstatusAsync_ExitosoRetornaEstadoVigente()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/api/status").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "estado": "Vigente",
                    "estatusCancelacion": "",
                    "esCancelable": true
                }
                """));

        var request = new ConsultaEstatusRequest(
            RfcEmisor: "XAXX010101000",
            RfcReceptor: "XEXX010101000",
            Total: "1500.00",
            Uuid: "6F4A20E7-9B4D-4C8A-A25F-3E7D6C9B8A1F");

        // Act
        var result = await _adapter.ConsultarEstatusAsync(request);

        // Assert
        Assert.Equal("Vigente", result.Estado);
        Assert.Equal("", result.EstatusCancelacion);
        Assert.True(result.EsCancelable);
    }
}

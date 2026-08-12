using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using McpCfdi.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpCfdi.Infrastructure.Pac;

/// <summary>
/// Adaptador PAC para el proveedor Multifacturas.
/// Traduce las operaciones de IPacService en llamadas REST JSON a la API de Multifacturas.
/// 
/// SEGURIDAD: Este adaptador NO registra en logs valores de CertificadoBase64,
/// LlavePrivadaBase64 ni PasswordLlave. Los request bodies de cancelación contienen
/// credenciales en el JSON — nunca se loguean cuerpos de request completos.
/// </summary>
public class MultifacturasPacAdapter : IPacService
{
    private const string PacProvider = "Multifacturas";
    private const string CorrelationIdHeader = "X-Correlation-Id";

    private readonly HttpClient _httpClient;
    private readonly MultifacturasPacOptions _options;
    private readonly ILogger<MultifacturasPacAdapter> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public MultifacturasPacAdapter(
        HttpClient httpClient,
        IOptions<PacOptions> options,
        ILogger<MultifacturasPacAdapter> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpClient = httpClient;
        _options = options.Value.Multifacturas;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TimbradoResult> TimbrarAsync(string cfdiXmlSellado, CancellationToken ct = default)
    {
        _logger.LogDebug("Iniciando timbrado con {PacProvider}", PacProvider);

        var xmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(cfdiXmlSellado));
        var payload = new { xml = xmlBase64 };

        SetCorrelationIdHeader();
        var response = await _httpClient.PostAsJsonAsync("/api/stamp", payload, ct);

        _logger.LogDebug("Respuesta de timbrado: {StatusCode}", response.StatusCode);

        await EnsureSuccessOrThrowAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<TimbradoResponse>(ct)
            ?? throw new PacIntegrationException("Respuesta vacía del PAC al timbrar", PacProvider);

        return new TimbradoResult(
            result.Uuid,
            result.FechaTimbrado,
            result.SelloSat,
            result.NoCertificadoSat,
            result.SelloCfd,
            result.CfdiTimbradoXml);
    }

    public async Task<CancelacionResult> CancelarAsync(CancelacionRequest request, CancellationToken ct = default)
    {
        _logger.LogDebug("Iniciando cancelación con {PacProvider} para UUID {Uuid}", PacProvider, request.Uuid);

        var payload = new
        {
            uuid = request.Uuid,
            rfc = request.RfcEmisor,
            motivo = request.Motivo,
            folioSustitucion = request.UuidSustitucion,
            b64Cer = request.CertificadoBase64,
            b64Key = request.LlavePrivadaBase64,
            password = request.PasswordLlave
        };

        // SEGURIDAD: No loguear el payload — contiene credenciales CSD (b64Cer, b64Key, password)
        SetCorrelationIdHeader();
        var response = await _httpClient.PostAsJsonAsync("/api/cancel", payload, ct);

        _logger.LogDebug("Respuesta de cancelación: {StatusCode}", response.StatusCode);

        await EnsureSuccessOrThrowAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<CancelacionResponse>(ct)
            ?? throw new PacIntegrationException("Respuesta vacía del PAC al cancelar", PacProvider);

        return new CancelacionResult(
            result.Uuid,
            result.EstatusUuid,
            result.AcuseXml,
            result.FechaCancelacion);
    }

    public async Task<EstatusCfdiResult> ConsultarEstatusAsync(ConsultaEstatusRequest request, CancellationToken ct = default)
    {
        _logger.LogDebug("Consultando estatus con {PacProvider} para UUID {Uuid}", PacProvider, request.Uuid);

        var url = $"/api/status?rfcEmisor={Uri.EscapeDataString(request.RfcEmisor)}" +
                  $"&rfcReceptor={Uri.EscapeDataString(request.RfcReceptor)}" +
                  $"&total={Uri.EscapeDataString(request.Total)}" +
                  $"&uuid={Uri.EscapeDataString(request.Uuid)}";

        SetCorrelationIdHeader();
        var response = await _httpClient.GetAsync(url, ct);

        _logger.LogDebug("Respuesta de consulta estatus: {StatusCode}", response.StatusCode);

        await EnsureSuccessOrThrowAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<EstatusResponse>(ct)
            ?? throw new PacIntegrationException("Respuesta vacía del PAC al consultar estatus", PacProvider);

        return new EstatusCfdiResult(
            result.Estado,
            result.EstatusCancelacion,
            result.EsCancelable);
    }

    /// <summary>
    /// Propaga el correlation ID del request HTTP original a las llamadas salientes al PAC.
    /// Usa el header X-Correlation-Id del request entrante, o HttpContext.TraceIdentifier como fallback.
    /// </summary>
    private void SetCorrelationIdHeader()
    {
        var correlationId = GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
        {
            // Remove existing header to avoid duplicates on reused HttpClient
            _httpClient.DefaultRequestHeaders.Remove(CorrelationIdHeader);
            _httpClient.DefaultRequestHeaders.Add(CorrelationIdHeader, correlationId);
        }
    }

    private string? GetCorrelationId()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext is null)
            return null;

        // Prefer explicit X-Correlation-Id header from incoming request
        if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
            && !string.IsNullOrEmpty(headerValue))
        {
            return headerValue!;
        }

        // Fallback to ASP.NET Core TraceIdentifier
        return httpContext.TraceIdentifier;
    }

    private async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var statusCode = (int)response.StatusCode;

        switch (statusCode)
        {
            case 400:
                var errorBody = await TryReadErrorResponseAsync(response, ct);
                _logger.LogWarning(
                    "Error de validación del PAC {PacProvider}: {Codigo} - {Detalle}",
                    PacProvider, errorBody?.Codigo, errorBody?.Detalle);
                throw new PacValidationException(
                    $"Error de validación del PAC: {errorBody?.Detalle ?? "Error desconocido"}",
                    PacProvider,
                    errorBody?.Codigo,
                    errorBody?.Detalle);

            case 401:
                _logger.LogWarning("Error de autenticación con el PAC {PacProvider}", PacProvider);
                throw new PacAuthenticationException(
                    "Credenciales inválidas para el PAC",
                    PacProvider);

            case 402:
                _logger.LogWarning("Créditos insuficientes en el PAC {PacProvider}", PacProvider);
                throw new PacInsufficientCreditsException(
                    "Créditos insuficientes en el PAC",
                    PacProvider);

            case >= 500:
                _logger.LogError(
                    "Error transitorio del PAC {PacProvider}: HTTP {StatusCode}",
                    PacProvider, statusCode);
                throw new PacTransientException(
                    $"Error del servidor PAC: HTTP {statusCode}",
                    PacProvider);

            default:
                _logger.LogError(
                    "Respuesta inesperada del PAC {PacProvider}: HTTP {StatusCode}",
                    PacProvider, statusCode);
                throw new PacIntegrationException(
                    $"Respuesta inesperada del PAC: HTTP {statusCode}",
                    PacProvider);
        }
    }

    private static async Task<PacErrorResponse?> TryReadErrorResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<PacErrorResponse>(ct);
        }
        catch
        {
            return null;
        }
    }

    #region Internal response models

    private sealed class TimbradoResponse
    {
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        [JsonPropertyName("fechaTimbrado")]
        public DateTime FechaTimbrado { get; set; }

        [JsonPropertyName("selloSat")]
        public string SelloSat { get; set; } = string.Empty;

        [JsonPropertyName("noCertificadoSat")]
        public string NoCertificadoSat { get; set; } = string.Empty;

        [JsonPropertyName("selloCfd")]
        public string SelloCfd { get; set; } = string.Empty;

        [JsonPropertyName("cfdiTimbradoXml")]
        public string CfdiTimbradoXml { get; set; } = string.Empty;
    }

    private sealed class CancelacionResponse
    {
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        [JsonPropertyName("estatusUuid")]
        public string EstatusUuid { get; set; } = string.Empty;

        [JsonPropertyName("acuseXml")]
        public string AcuseXml { get; set; } = string.Empty;

        [JsonPropertyName("fechaCancelacion")]
        public DateTime FechaCancelacion { get; set; }
    }

    private sealed class EstatusResponse
    {
        [JsonPropertyName("estado")]
        public string Estado { get; set; } = string.Empty;

        [JsonPropertyName("estatusCancelacion")]
        public string EstatusCancelacion { get; set; } = string.Empty;

        [JsonPropertyName("esCancelable")]
        public bool EsCancelable { get; set; }
    }

    private sealed class PacErrorResponse
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }

        [JsonPropertyName("detalle")]
        public string? Detalle { get; set; }
    }

    #endregion
}

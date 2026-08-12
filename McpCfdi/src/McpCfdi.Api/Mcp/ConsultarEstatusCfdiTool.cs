using System.ComponentModel;
using FluentValidation;
using MediatR;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using McpCfdi.Application.Queries.ConsultarEstatusCfdi;
using McpCfdi.Infrastructure.Exceptions;

namespace McpCfdi.Api.Mcp;

/// <summary>
/// MCP tool that queries the status of a CFDI (Vigente, Cancelado, or No encontrado).
/// Delegates to ConsultarEstatusCfdiQuery via MediatR and maps exceptions to MCP error responses.
/// </summary>
[McpServerToolType]
public class ConsultarEstatusCfdiTool
{
    [McpServerTool(Name = "consultar_estatus_cfdi"), Description("Consulta si un CFDI está Vigente, Cancelado o No encontrado.")]
    public async Task<CallToolResponse> ConsultarAsync(
        ISender mediator,
        [Description("RFC del emisor del comprobante")] string RfcEmisor,
        [Description("RFC del receptor del comprobante")] string RfcReceptor,
        [Description("Total del comprobante (ej: 1234.56)")] string Total,
        [Description("UUID (folio fiscal) del comprobante a consultar")] string Uuid,
        CancellationToken ct)
    {
        var query = new ConsultarEstatusCfdiQuery
        {
            RfcEmisor = RfcEmisor,
            RfcReceptor = RfcReceptor,
            Total = Total,
            Uuid = Uuid
        };

        try
        {
            var result = await mediator.Send(query, ct);

            var responseText = $"""
                Estatus del CFDI {Uuid}:
                - Estado: {result.Estado}
                - Estatus de cancelación: {result.EstatusCancelacion}
                - Es cancelable: {(result.EsCancelable ? "Sí" : "No")}
                """;

            return new CallToolResponse
            {
                Content = [new Content { Type = "text", Text = responseText }]
            };
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("\n", ex.Errors.Select(e => $"- {e.PropertyName}: {e.ErrorMessage}"));
            return CreateErrorResponse($"Error de validación al consultar estatus:\n{errors}");
        }
        catch (PacValidationException ex)
        {
            return CreateErrorResponse($"Error de validación PAC [{ex.CodigoError}]: {ex.DetalleError ?? ex.Message}");
        }
        catch (PacAuthenticationException ex)
        {
            return CreateErrorResponse($"Error de autenticación con el PAC: {ex.Message}");
        }
        catch (PacInsufficientCreditsException ex)
        {
            return CreateErrorResponse($"Créditos insuficientes en el PAC: {ex.Message}");
        }
        catch (PacTransientException ex)
        {
            return CreateErrorResponse($"Error transitorio del PAC (reintentable): {ex.Message}");
        }
        catch (PacUnavailableException ex)
        {
            return CreateErrorResponse($"PAC no disponible (circuit breaker abierto): {ex.Message}");
        }
        catch (PacIntegrationException ex)
        {
            return CreateErrorResponse($"Error de integración con el PAC: {ex.Message}");
        }
        catch (EmisorCredencialesException ex)
        {
            return CreateErrorResponse($"Error de configuración de credenciales del emisor: {ex.Message}");
        }
    }

    private static CallToolResponse CreateErrorResponse(string message)
    {
        return new CallToolResponse
        {
            IsError = true,
            Content = [new Content { Type = "text", Text = message }]
        };
    }
}

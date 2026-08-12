using System.ComponentModel;
using FluentValidation;
using MediatR;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using McpCfdi.Application.Commands.CancelarCfdi;
using McpCfdi.Infrastructure.Exceptions;

namespace McpCfdi.Api.Mcp;

/// <summary>
/// MCP tool that cancels a previously stamped CFDI via PAC.
/// Delegates to CancelarCfdiCommand via MediatR and maps exceptions to MCP error responses.
/// </summary>
[McpServerToolType]
public class CancelarCfdiTool
{
    [McpServerTool(Name = "cancelar_cfdi"), Description("Cancela un CFDI timbrado. Requiere motivo (01-04). Las credenciales CSD se cargan automáticamente por RFC desde la configuración local.")]
    public async Task<CallToolResponse> CancelarAsync(
        ISender mediator,
        [Description("UUID (folio fiscal) del CFDI a cancelar")] string Uuid,
        [Description("RFC del emisor del comprobante")] string RfcEmisor,
        [Description("Motivo de cancelación según catálogo SAT: 01, 02, 03 o 04")] string Motivo,
        CancellationToken ct,
        [Description("UUID del CFDI que sustituye al cancelado. Obligatorio cuando Motivo es 01")] string? UuidSustitucion = null)
    {
        var command = new CancelarCfdiCommand
        {
            Uuid = Uuid,
            RfcEmisor = RfcEmisor,
            Motivo = Motivo,
            UuidSustitucion = UuidSustitucion
        };

        try
        {
            var result = await mediator.Send(command, ct);

            var responseText = $"""
                Cancelación exitosa:
                - UUID: {result.Uuid}
                - Estatus: {result.EstatusUuid}
                - Fecha de cancelación: {result.FechaCancelacion:yyyy-MM-dd HH:mm:ss}
                - Acuse XML incluido en respuesta
                
                {result.AcuseXml}
                """;

            return new CallToolResponse
            {
                Content = [new Content { Type = "text", Text = responseText }]
            };
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("\n", ex.Errors.Select(e => $"- {e.PropertyName}: {e.ErrorMessage}"));
            return CreateErrorResponse($"Error de validación al cancelar CFDI:\n{errors}");
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

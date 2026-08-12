using System.ComponentModel;
using FluentValidation;
using MediatR;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using McpCfdi.Application.Commands.TimbrarCfdi;
using McpCfdi.Infrastructure.Exceptions;

namespace McpCfdi.Api.Mcp;

/// <summary>
/// MCP tool that stamps a pre-sealed CFDI 4.0 XML via a PAC to obtain the UUID (Timbre Fiscal Digital).
/// Delegates to TimbrarCfdiCommand via MediatR and maps exceptions to MCP error responses.
/// </summary>
[McpServerToolType]
public class TimbrarCfdiTool
{
    [McpServerTool(Name = "timbrar_cfdi"), Description("Envía un XML de CFDI pre-sellado al PAC para obtener el Timbre Fiscal Digital (UUID).")]
    public async Task<CallToolResponse> TimbrarAsync(
        ISender mediator,
        [Description("XML completo del CFDI 4.0 pre-sellado (con Sello, NoCertificado, Certificado)")] string CfdiXmlSellado,
        CancellationToken ct)
    {
        var command = new TimbrarCfdiCommand
        {
            CfdiXmlSellado = CfdiXmlSellado
        };

        try
        {
            var result = await mediator.Send(command, ct);

            return new CallToolResponse
            {
                Content = [new Content { Type = "text", Text = result.CfdiTimbradoXml }]
            };
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("\n", ex.Errors.Select(e => $"- {e.PropertyName}: {e.ErrorMessage}"));
            return CreateErrorResponse($"Error de validación al timbrar CFDI:\n{errors}");
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

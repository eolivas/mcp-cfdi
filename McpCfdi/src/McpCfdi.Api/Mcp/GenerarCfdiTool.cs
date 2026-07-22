using System.ComponentModel;
using FluentValidation;
using MediatR;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using McpCfdi.Application.Commands.GenerarCfdi;
using McpCfdi.Application.DTOs;
using McpCfdi.Domain.Exceptions;
using McpCfdi.Infrastructure.Exceptions;

namespace McpCfdi.Api.Mcp;

/// <summary>
/// MCP tool that generates a valid CFDI 4.0 document conforming to SAT Anexo 20.
/// Delegates to GenerarCfdiCommand via MediatR and maps exceptions to MCP error responses.
/// </summary>
[McpServerToolType]
public class GenerarCfdiTool
{
    [McpServerTool(Name = "generar_cfdi"), Description("Genera un CFDI 4.0 válido conforme al Anexo 20 del SAT")]
    public async Task<CallToolResponse> GenerarAsync(
        ISender mediator,
        EmisorDto Emisor,
        ReceptorDto Receptor,
        IReadOnlyList<ConceptoDto> Conceptos,
        string FormaPago,
        string MetodoPago,
        string Moneda,
        string LugarExpedicion,
        string Exportacion,
        CancellationToken ct,
        DateTime? Fecha = null,
        byte[]? LlavePrivadaDer = null,
        string? PasswordLlave = null,
        byte[]? CertificadoDer = null)
    {
        var command = new GenerarCfdiCommand
        {
            Emisor = Emisor,
            Receptor = Receptor,
            Conceptos = Conceptos,
            FormaPago = FormaPago,
            MetodoPago = MetodoPago,
            Moneda = Moneda,
            LugarExpedicion = LugarExpedicion,
            Exportacion = Exportacion,
            Fecha = Fecha,
            LlavePrivadaDer = LlavePrivadaDer,
            PasswordLlave = PasswordLlave,
            CertificadoDer = CertificadoDer
        };

        try
        {
            var result = await mediator.Send(command, ct);

            return new CallToolResponse
            {
                Content = [new Content { Type = "text", Text = result.Xml }]
            };
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("\n", ex.Errors.Select(e => $"- {e.PropertyName}: {e.ErrorMessage}"));
            return CreateErrorResponse($"Error de validación al generar CFDI:\n{errors}");
        }
        catch (InvalidRfcException ex)
        {
            return CreateErrorResponse($"Error de dominio: {ex.Message}");
        }
        catch (InvalidMontoException ex)
        {
            return CreateErrorResponse($"Error de dominio: {ex.Message}");
        }
        catch (MissingMandatoryFieldException ex)
        {
            return CreateErrorResponse($"Error de dominio: {ex.Message}");
        }
        catch (CatalogoUnavailableException ex)
        {
            return CreateErrorResponse($"Error de infraestructura: No se pudo consultar el catálogo '{ex.Catalogo}'. {ex.Message}");
        }
        catch (XsltTransformException ex)
        {
            return CreateErrorResponse($"Error de infraestructura: Falla en transformación XSLT. {ex.Message}");
        }
        catch (SigningException ex)
        {
            return CreateErrorResponse($"Error de infraestructura: Falla al firmar el CFDI. {ex.Message}");
        }
        catch (XmlParsingException ex)
        {
            return CreateErrorResponse($"Error de infraestructura: Falla al procesar XML. {ex.Message}");
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

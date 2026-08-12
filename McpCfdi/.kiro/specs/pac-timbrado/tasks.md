# Implementation Plan: PAC Timbrado

## Overview

Implementación de la integración con PAC (Proveedor Autorizado de Certificación) para timbrado, cancelación y consulta de estatus de CFDI 4.0. La arquitectura sigue Clean Architecture con Strategy pattern para el PAC activo, Decorator para resiliencia, y adaptador concreto para Multifacturas como implementación inicial.

## Tasks

- [x] 1. Definir excepciones de dominio, interfaz IPacService y DTOs
  - [x] 1.1 Crear la jerarquía de excepciones PAC en Infrastructure
    - Crear `src/McpCfdi.Infrastructure/Exceptions/PacException.cs` con la clase base abstracta `PacException`
    - Crear `PacTransientException`, `PacValidationException`, `PacAuthenticationException`, `PacInsufficientCreditsException`, `PacIntegrationException`, `PacUnavailableException` y `EmisorCredencialesException`
    - Cada excepción incluye propiedades adicionales según diseño (CodigoError, DetalleError, PacProvider)
    - _Requirements: 5.3, 8.1, 8.2_

  - [x] 1.2 Crear la interfaz IPacService y los DTOs de resultado en Application
    - Crear `src/McpCfdi.Application/Interfaces/IPacService.cs` con métodos `TimbrarAsync`, `CancelarAsync`, `ConsultarEstatusAsync`
    - Crear `src/McpCfdi.Application/DTOs/TimbradoResult.cs`, `CancelacionResult.cs`, `EstatusCfdiResult.cs`, `CancelacionRequest.cs`, `ConsultaEstatusRequest.cs`, `EmisorCredenciales.cs`
    - Todos como records inmutables
    - _Requirements: 1.2, 1.3, 2.2, 3.2, 4.1_

  - [x] 1.3 Crear la interfaz IEmisorCredencialesProvider en Application
    - Crear `src/McpCfdi.Application/Interfaces/IEmisorCredencialesProvider.cs` con métodos `ObtenerCredencialesAsync` y `ExistenCredenciales`
    - _Requirements: 6.1, 6.2_

- [x] 2. Implementar Commands, Queries y Validators
  - [x] 2.1 Crear TimbrarCfdiCommand y su Validator
    - Crear `src/McpCfdi.Application/Commands/TimbrarCfdi/TimbrarCfdiCommand.cs` como `IRequest<TimbradoResult>`
    - Crear `src/McpCfdi.Application/Commands/TimbrarCfdi/TimbrarCfdiCommandValidator.cs` con reglas: XML no vacío, debe contener atributos Sello, NoCertificado, Certificado con valores no vacíos
    - _Requirements: 1.1, 1.4_

  - [x] 2.2 Crear CancelarCfdiCommand y su Validator
    - Crear `src/McpCfdi.Application/Commands/CancelarCfdi/CancelarCfdiCommand.cs` como `IRequest<CancelacionResult>`
    - Crear `src/McpCfdi.Application/Commands/CancelarCfdi/CancelarCfdiCommandValidator.cs` con reglas: UUID formato GUID, RfcEmisor no vacío, Motivo en [01,02,03,04], UuidSustitucion obligatorio cuando Motivo=="01"
    - _Requirements: 2.1, 2.3_

  - [x] 2.3 Crear ConsultarEstatusCfdiQuery y su Validator
    - Crear `src/McpCfdi.Application/Queries/ConsultarEstatusCfdi/ConsultarEstatusCfdiQuery.cs` como `IRequest<EstatusCfdiResult>`
    - Crear `src/McpCfdi.Application/Queries/ConsultarEstatusCfdi/ConsultarEstatusCfdiQueryValidator.cs` con reglas: RfcEmisor, RfcReceptor, Total, Uuid todos no vacíos, Uuid formato GUID
    - _Requirements: 3.1_

  - [x] 2.4 Write property tests for validators (Properties 2, 3)
    - **Property 2: Validación pre-envío bloquea XML sin sello** — Para cualquier XML que NO contenga Sello, NoCertificado y Certificado con valores no vacíos, TimbrarCfdiCommandValidator debe rechazar con errores de validación
    - **Property 3: Motivo 01 requiere UUID de sustitución** — Para cualquier CancelarCfdiCommand con Motivo=="01" y UuidSustitucion null/vacío, el validator debe rechazar. Para Motivos "02","03","04" debe aceptar sin UuidSustitucion
    - Usar FsCheck con xUnit para generación de entradas arbitrarias
    - Crear tests en `tests/McpCfdi.Application.Tests/Commands/TimbrarCfdi/` y `Commands/CancelarCfdi/`
    - **Validates: Requirements 1.4, 2.3**

- [x] 3. Implementar Handlers
  - [x] 3.1 Crear TimbrarCfdiCommandHandler
    - Crear `src/McpCfdi.Application/Commands/TimbrarCfdi/TimbrarCfdiCommandHandler.cs`
    - Inyecta `IPacService`, delega a `TimbrarAsync`
    - _Requirements: 1.1, 1.2_

  - [x] 3.2 Crear CancelarCfdiCommandHandler
    - Crear `src/McpCfdi.Application/Commands/CancelarCfdi/CancelarCfdiCommandHandler.cs`
    - Inyecta `IPacService` y `IEmisorCredencialesProvider`
    - Carga credenciales CSD por RFC, construye `CancelacionRequest`, delega a `CancelarAsync`
    - _Requirements: 2.1, 6.1, 6.2_

  - [x] 3.3 Crear ConsultarEstatusCfdiQueryHandler
    - Crear `src/McpCfdi.Application/Queries/ConsultarEstatusCfdi/ConsultarEstatusCfdiQueryHandler.cs`
    - Inyecta `IPacService`, construye `ConsultaEstatusRequest`, delega a `ConsultarEstatusAsync`
    - _Requirements: 3.1, 3.2_

  - [x] 3.4 Write unit tests for handlers
    - Usar NSubstitute para mockear `IPacService` e `IEmisorCredencialesProvider`
    - Verificar que TimbrarCfdiCommandHandler delega correctamente
    - Verificar que CancelarCfdiCommandHandler carga credenciales y construye CancelacionRequest correctamente
    - Verificar que ConsultarEstatusCfdiQueryHandler construye ConsultaEstatusRequest correctamente
    - _Requirements: 1.1, 2.1, 3.1_

- [x] 4. Checkpoint - Validar capa de Application
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implementar configuración y credenciales del emisor
  - [x] 5.1 Crear clases de configuración PacOptions y EmisoresOptions
    - Crear `src/McpCfdi.Infrastructure/Pac/PacOptions.cs` con `ActiveProvider`, secciones `MultifacturasPacOptions` y `FiscalCloudPacOptions`
    - Crear `src/McpCfdi.Infrastructure/Pac/EmisoresOptions.cs` con `CertificadosDir` y `DefaultRfc`
    - _Requirements: 11.1, 11.2_

  - [x] 5.2 Implementar FileSystemEmisorCredencialesProvider
    - Crear `src/McpCfdi.Infrastructure/Pac/FileSystemEmisorCredencialesProvider.cs`
    - Carga certificado.cer y llave.key desde `{CertificadosDir}/{RFC}/`
    - Lee password de variable de entorno `EMISOR__{RFC}__PASSWORD_LLAVE`
    - Lanza `EmisorCredencialesException` si faltan archivos o variable de entorno
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [x] 5.3 Write unit tests for FileSystemEmisorCredencialesProvider
    - Test con archivos existentes retorna credenciales correctas
    - Test con certificado faltante lanza EmisorCredencialesException
    - Test con variable de entorno faltante lanza EmisorCredencialesException
    - _Requirements: 6.4_

- [x] 6. Implementar PacServiceFactory y PacResilienceDecorator
  - [x] 6.1 Crear PacServiceFactory
    - Crear `src/McpCfdi.Infrastructure/Pac/PacServiceFactory.cs`
    - Resuelve implementación de IPacService según `PacOptions.ActiveProvider`
    - Envuelve el adaptador resuelto con `PacResilienceDecorator`
    - Lanza `InvalidOperationException` para provider desconocido
    - _Requirements: 4.2, 4.4, 11.2_

  - [x] 6.2 Crear PacResilienceDecorator
    - Crear `src/McpCfdi.Infrastructure/Pac/PacResilienceDecorator.cs` implementando `IPacService`
    - Retry con backoff exponencial (3 reintentos) solo para `PacTransientException`
    - Circuit breaker: abre tras 5 fallos consecutivos, cierra tras 30s
    - No reintenta errores 4xx (PacValidationException, PacAuthenticationException, etc.)
    - _Requirements: 8.1, 8.2, 8.3_

  - [x] 6.3 Write property tests for PacResilienceDecorator (Properties 4, 5)
    - **Property 4: Retry solo en errores transitorios** — Para cualquier PacTransientException, el decorator reintenta hasta 3 veces. Para cualquier PacValidationException, propaga inmediatamente sin retry
    - **Property 5: Circuit breaker se abre tras 5 fallos consecutivos** — Para secuencias de 5+ PacTransientException consecutivas, las llamadas posteriores lanzan PacUnavailableException sin contactar al inner service
    - Usar FsCheck para generar secuencias arbitrarias de excepciones y verificar comportamiento
    - Crear tests en `tests/McpCfdi.Infrastructure.Tests/Pac/`
    - **Validates: Requirements 8.1, 8.2**

  - [x] 6.4 Write property test for PacServiceFactory (Property 6)
    - **Property 6: Cambio de PAC por configuración** — Para cualquier valor válido de ActiveProvider ("Multifacturas", "FiscalCloud"), el factory resuelve la implementación correspondiente. Para valor inválido, lanza InvalidOperationException
    - Crear test en `tests/McpCfdi.Infrastructure.Tests/Pac/`
    - **Validates: Requirements 4.2, 11.2**

- [x] 7. Implementar adaptador MultifacturasPacAdapter
  - [x] 7.1 Crear MultifacturasPacAdapter
    - Crear `src/McpCfdi.Infrastructure/Pac/MultifacturasPacAdapter.cs` implementando `IPacService`
    - Implementar `TimbrarAsync`: POST /api/stamp con XML en base64, parsear respuesta JSON a TimbradoResult
    - Implementar `CancelarAsync`: POST /api/cancel con uuid, rfc, motivo, folioSustitucion, credenciales CSD
    - Implementar `ConsultarEstatusAsync`: GET /api/status con query params
    - Mapear códigos HTTP: 200→éxito, 400→PacValidationException, 401→PacAuthenticationException, 402→PacInsufficientCreditsException, 5xx→PacTransientException
    - Logging a nivel Debug para requests y Warning/Error para errores
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 9.3_

  - [x] 7.2 Write integration tests for MultifacturasPacAdapter con WireMock
    - Test timbrado exitoso retorna UUID y XML con TimbreFiscalDigital
    - Test error 500 lanza PacTransientException
    - Test error 400 lanza PacValidationException con código y detalle
    - Test error 401 lanza PacAuthenticationException
    - Test error 402 lanza PacInsufficientCreditsException
    - Test cancelación exitosa retorna acuse y estatus
    - Test consulta estatus exitosa retorna estado vigente/cancelado
    - Usar WireMock.Net para simular API de Multifacturas
    - _Requirements: 5.1, 5.3_

  - [x] 7.3 Write property test for timbrado response parsing (Property 1)
    - **Property 1: Respuesta de timbrado contiene TimbreFiscalDigital** — Para cualquier respuesta exitosa del adaptador, el CfdiTimbradoXml contiene nodo tfd:TimbreFiscalDigital con UUID, FechaTimbrado, SelloCFD, NoCertificadoSAT, SelloSAT y Version="1.1" todos no vacíos
    - Crear test en `tests/McpCfdi.Infrastructure.Tests/Pac/`
    - **Validates: Requirements 1.2, 1.3**

- [~] 8. Checkpoint - Validar capa de Infrastructure
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Implementar MCP Tools y registro de DI
  - [x] 9.1 Crear MCP Tools para PAC
    - Crear `src/McpCfdi.Api/Mcp/TimbrarCfdiTool.cs` — recibe XML sellado, envía TimbrarCfdiCommand vía MediatR
    - Crear `src/McpCfdi.Api/Mcp/CancelarCfdiTool.cs` — recibe uuid, rfcEmisor, motivo, uuidSustitucion, envía CancelarCfdiCommand
    - Crear `src/McpCfdi.Api/Mcp/ConsultarEstatusCfdiTool.cs` — recibe rfcEmisor, rfcReceptor, total, uuid, envía ConsultarEstatusCfdiQuery
    - Cada tool con atributos `[McpServerTool]` y `[Description]` descriptivos en español
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [x] 9.2 Crear extensión AddPacServices para DI
    - Crear `src/McpCfdi.Infrastructure/Pac/DependencyInjection.cs` con método de extensión `AddPacServices(IServiceCollection, IConfiguration)`
    - Registrar PacOptions y EmisoresOptions desde IConfiguration
    - Registrar FileSystemEmisorCredencialesProvider como Singleton
    - Registrar HttpClient para MultifacturasPacAdapter con BaseUrl, timeout y Authorization header desde opciones
    - Registrar PacServiceFactory y resolver IPacService como Singleton
    - _Requirements: 4.2, 4.4, 5.4, 11.1_

  - [x] 9.3 Integrar AddPacServices en Program.cs
    - Agregar llamada a `builder.Services.AddPacServices(builder.Configuration)` en `src/McpCfdi.Api/Program.cs`
    - Agregar sección de configuración PAC y Emisores en `appsettings.json` (con placeholders para API keys)
    - _Requirements: 11.1, 11.3_

  - [x] 9.4 Write unit tests for MCP Tools
    - Verificar que cada tool delega correctamente al MediatR ISender
    - Verificar que los parámetros se mapean al command/query correcto
    - Usar NSubstitute para mockear ISender
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 10. Observabilidad y seguridad
  - [x] 10.1 Agregar logging estructurado y métricas a las llamadas PAC
    - Agregar logging de latencia y resultado en PacResilienceDecorator (métricas por operación)
    - Propagar correlation ID existente del pipeline en las llamadas HTTP (header X-Correlation-Id)
    - Verificar que ningún log incluye credenciales, llaves privadas ni certificados
    - _Requirements: 9.1, 9.2, 9.3, 10.2_

  - [x] 10.2 Write property test for credential scrubbing in logs (Property 7)
    - **Property 7: Credenciales no aparecen en logs** — Para cualquier llamada que involucre LlavePrivadaBase64, PasswordLlave o CertificadoBase64, estos valores NO aparecen en ningún mensaje de log
    - Usar un ILogger mock que capture mensajes y verificar con FsCheck que strings de credenciales generados arbitrariamente nunca aparecen en la salida
    - Crear test en `tests/McpCfdi.Infrastructure.Tests/Pac/`
    - **Validates: Requirements 10.2**

- [~] 11. Final checkpoint - Validar integración completa
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties defined in the design document using FsCheck
- Unit tests validate specific examples and edge cases using xUnit + NSubstitute
- Integration tests use WireMock.Net to simulate PAC API responses
- All code uses C# 12 / .NET 8, consistent with the existing solution
- The credential files (.cer/.key) and environment variables are expected to exist in the deployment environment — tests mock these dependencies

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["2.1", "2.2", "2.3", "5.1"] },
    { "id": 2, "tasks": ["2.4", "3.1", "3.2", "3.3", "5.2"] },
    { "id": 3, "tasks": ["3.4", "5.3", "6.1", "6.2"] },
    { "id": 4, "tasks": ["6.3", "6.4", "7.1"] },
    { "id": 5, "tasks": ["7.2", "7.3", "9.1", "9.2"] },
    { "id": 6, "tasks": ["9.3", "9.4", "10.1"] },
    { "id": 7, "tasks": ["10.2"] }
  ]
}
```

# Implementation Plan: CFDI 4.0 Generation

## Overview

This plan implements the CFDI (Comprobante Fiscal Digital por Internet) version 4.0 generation feature following Clean Architecture. The implementation builds from domain Value Objects upward through the Application layer command/handler and Infrastructure services, culminating in MCP tool exposure. Each step is incremental and testable.

## Tasks

- [x] 1. Implement Domain Value Objects
  - [x] 1.1 Create `Rfc` value object in `McpCfdi.Domain/ValueObjects/Rfc.cs`
    - Implement regex validation for persona física (13 chars) and persona moral (12 chars)
    - Support generic RFCs: `XAXX010101000`, `XEXX010101000`
    - Expose `TipoPersona` enum (Fisica, Moral, Generico) and `EsGenerico` property
    - Throw `InvalidRfcException` for invalid inputs
    - _Requirements: 2.2, 3.2_

  - [x] 1.2 Write property test for `Rfc` value object
    - **Property 9: Validación de estructura de RFC**
    - **Validates: Requirements 2.2, 3.2**
    - Use FsCheck `Arb<string>` to generate arbitrary strings and verify only SAT-compliant patterns are accepted

  - [x] 1.3 Create `MontoMoneda` value object in `McpCfdi.Domain/ValueObjects/MontoMoneda.cs`
    - Implement rounding with `MidpointRounding.AwayFromZero` per currency decimals
    - Implement `+` operator, `*` operator with decimal factor
    - Implement `FormatearParaXml()` returning exact decimal places (e.g., `F2` for MXN)
    - Throw `InvalidMontoException` for negative values
    - _Requirements: 7.6, 8.5_

  - [x] 1.4 Create `ClaveCatalogo` value object in `McpCfdi.Domain/ValueObjects/ClaveCatalogo.cs`
    - Validate non-empty `Catalogo` and `Clave` strings
    - _Requirements: 11.3_

  - [x] 1.5 Create `CodigoPostal` value object in `McpCfdi.Domain/ValueObjects/CodigoPostal.cs`
    - Validate exactly 5 numeric digits via regex
    - _Requirements: 1.3, 3.3_

  - [x] 1.6 Write unit tests for `MontoMoneda`, `ClaveCatalogo`, and `CodigoPostal`
    - Test rounding behavior, operator overloads, XML formatting
    - Test rejection of invalid inputs (negative values, empty strings, non-5-digit postal codes)
    - _Requirements: 7.6, 8.5, 11.3, 3.3_

- [x] 2. Implement Domain Entities and Aggregate Root
  - [x] 2.1 Create `Emisor` entity in `McpCfdi.Domain/Entities/Emisor.cs`
    - Include `Rfc`, `Nombre` (1-254 chars), `RegimenFiscal` (ClaveCatalogo)
    - Validate name length constraints in constructor
    - _Requirements: 2.1, 2.3, 2.7_

  - [x] 2.2 Create `Receptor` entity in `McpCfdi.Domain/Entities/Receptor.cs`
    - Include `Rfc`, `Nombre`, `DomicilioFiscalReceptor` (CodigoPostal), `RegimenFiscalReceptor`, `UsoCfdi`
    - Validate name length constraints in constructor
    - _Requirements: 3.1, 3.3, 3.4, 3.5_

  - [x] 2.3 Create `TrasladoConcepto` entity in `McpCfdi.Domain/Entities/TrasladoConcepto.cs`
    - Include `Base`, `Impuesto`, `TipoFactor`, `TasaOCuota?`, `Importe?`
    - Enforce: if TipoFactor is "Exento", TasaOCuota and Importe must be null
    - _Requirements: 5.2, 5.3_

  - [x] 2.4 Create `Concepto` entity in `McpCfdi.Domain/Entities/Concepto.cs`
    - Include all mandatory attributes: `ClaveProdServ`, `Cantidad`, `ClaveUnidad`, `Descripcion`, `ValorUnitario`, `Importe`, `ObjetoImp`
    - Include optional: `NoIdentificacion` (max 100 chars), `Unidad` (max 20 chars), `Descuento`
    - Include `Traslados` collection
    - Validate Cantidad > 0, max 6 decimals; Descripcion 1-1000 chars
    - _Requirements: 4.2, 4.8, 4.9, 5.1_

  - [x] 2.5 Create `ImpuestosGlobal` and `TrasladoGlobal` value objects in `McpCfdi.Domain/ValueObjects/`
    - `ImpuestosGlobal` with `TotalImpuestosTrasladados` and list of `TrasladoGlobal`
    - `TrasladoGlobal` with `Base`, `Impuesto`, `TipoFactor`, `TasaOCuota?`, `Importe?`
    - _Requirements: 6.1, 6.3, 6.4_

  - [x] 2.6 Create `Comprobante` aggregate root in `McpCfdi.Domain/Entities/Comprobante.cs`
    - Implement `Crear()` factory method with all mandatory parameters
    - Implement `CalcularTotales(int decimalesMoneda)` computing SubTotal, Descuento, global taxes, and Total
    - Implement `AsignarSello(string sello, string certificado, string noCertificado)`
    - Enforce invariant: at least one Concepto required
    - _Requirements: 1.3, 1.6, 4.1, 6.1, 6.2, 7.1, 7.2, 7.3_

  - [x] 2.7 Write property test for Importe calculation
    - **Property 3: Importe de concepto = Cantidad × ValorUnitario redondeado**
    - **Validates: Requirements 4.5, 5.5**
    - Use FsCheck `ArbCantidad` and `ArbMontoMoneda` generators

  - [x] 2.8 Write property test for global tax totals
    - **Property 4: Totales globales de impuestos = suma de importes a nivel concepto**
    - **Validates: Requirements 6.5, 6.6, 6.7**
    - Use FsCheck `ArbComprobante` generator

  - [x] 2.9 Write property test for comprobante totals formula
    - **Property 5: Fórmula de totales del comprobante**
    - **Validates: Requirements 7.1, 7.2, 7.3**
    - Use FsCheck `ArbComprobante` generator

- [x] 3. Implement Domain Interfaces (Ports)
  - [x] 3.1 Create `ICatalogoSatService` interface in `McpCfdi.Domain/Interfaces/ICatalogoSatService.cs`
    - Define `ExisteClaveAsync`, `ValidarClavesAsync`, `ObtenerDecimalesMonedaAsync`
    - _Requirements: 11.1, 11.4_

  - [x] 3.2 Create `ICfdiSerializer` interface in `McpCfdi.Domain/Interfaces/ICfdiSerializer.cs`
    - Define `Serializar`, `Deserializar`, `SerializarAString`
    - _Requirements: 8.1, 9.1_

  - [x] 3.3 Create `ICadenaOriginalGenerator` interface in `McpCfdi.Domain/Interfaces/ICadenaOriginalGenerator.cs`
    - Define `Generar(XDocument cfdiXml): string`
    - _Requirements: 10.1_

  - [x] 3.4 Create `ISelloDigitalService` interface in `McpCfdi.Domain/Interfaces/ISelloDigitalService.cs`
    - Define `Firmar`, `ObtenerNoCertificado`, `ObtenerCertificadoBase64`
    - _Requirements: 10.5_

- [x] 4. Implement Domain Exceptions
  - [x] 4.1 Create domain exception classes in `McpCfdi.Domain/Exceptions/`
    - `InvalidRfcException`, `InvalidMontoException`, `MissingMandatoryFieldException`
    - Include contextual error messages with field names and values
    - _Requirements: 1.6, 2.5, 2.6, 2.7, 3.6, 3.7_

- [x] 5. Checkpoint - Domain layer complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implement Infrastructure - XML Serialization
  - [x] 6.1 Create `CfdiXmlSerializer` in `McpCfdi.Infrastructure/Xml/CfdiXmlSerializer.cs`
    - Implement `ICfdiSerializer` using `System.Xml.Linq` (XElement/XDocument)
    - Set namespace `http://www.sat.gob.mx/cfd/4` with prefix `cfdi`
    - Include `xsi:schemaLocation` attribute
    - Omit `<?xml?>` declaration; output UTF-8
    - Serialize numeric values with exact decimal places per currency
    - Omit null optional attributes (no empty attributes)
    - Enforce XSD child node order: InformacionGlobal, CfdiRelacionados, Emisor, Receptor, Conceptos, Impuestos, Complemento, Addenda
    - Format `Fecha` as `yyyy-MM-ddTHH:mm:ss` without timezone
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 1.4_

  - [x] 6.2 Implement `Deserializar` method in `CfdiXmlSerializer`
    - Parse XDocument back to `Comprobante` aggregate
    - Validate namespace `http://www.sat.gob.mx/cfd/4` presence
    - Validate mandatory attributes exist
    - Throw `XmlParsingException` for malformed XML or wrong namespace
    - _Requirements: 9.1, 9.2, 9.3, 9.5_

  - [x] 6.3 Write property test for serialization round-trip
    - **Property 1: Round-trip de serialización/deserialización**
    - **Validates: Requirements 9.1, 9.4, 9.5**
    - Use FsCheck `ArbComprobante` generator

  - [x] 6.4 Write property test for mandatory XML elements
    - **Property 2: XML serializado contiene todos los elementos obligatorios**
    - **Validates: Requirements 1.1, 1.3, 2.1, 3.1, 4.2**
    - Use FsCheck `ArbComprobante` generator

  - [x] 6.5 Write property test for decimal formatting
    - **Property 6: Formateo numérico respeta decimales de la moneda**
    - **Validates: Requirements 7.6, 8.5**
    - Use FsCheck `ArbMontoMoneda` generator

  - [x] 6.6 Write property test for TipoFactor attribute presence
    - **Property 7: TipoFactor determina conjunto de atributos del traslado**
    - **Validates: Requirements 5.2, 5.3, 6.4**
    - Use FsCheck `ArbTrasladoConcepto` generator

  - [x] 6.7 Write property test for optional attributes omission
    - **Property 14: Atributos opcionales ausentes se omiten del XML**
    - **Validates: Requirements 8.6**
    - Use FsCheck `ArbComprobante` with random null optionals

  - [x] 6.8 Write property test for XSD node order
    - **Property 15: Orden de nodos hijo conforme al XSD**
    - **Validates: Requirements 8.8**
    - Use FsCheck `ArbComprobante` generator

  - [x] 6.9 Write property test for Fecha format
    - **Property 16: Formato de fecha sin zona horaria**
    - **Validates: Requirements 1.4**
    - Use FsCheck `Arb<DateTime>` generator

  - [x] 6.10 Write property test for ObjetoImp and Impuestos node
    - **Property 13: ObjetoImp="02" implica existencia de nodo Impuestos**
    - **Validates: Requirements 5.1, 6.1, 6.2**
    - Use FsCheck `ArbComprobante` generator

- [x] 7. Implement Infrastructure - Cadena Original and Sello Digital
  - [x] 7.1 Create `XsltCadenaOriginalGenerator` in `McpCfdi.Infrastructure/Xml/XsltCadenaOriginalGenerator.cs`
    - Implement `ICadenaOriginalGenerator`
    - Embed `cadenaoriginal_4_0.xslt` as assembly resource
    - Apply XSLT transformation to XDocument
    - Throw `XsltTransformException` on failure
    - _Requirements: 10.1, 10.2, 10.6_

  - [x] 7.2 Create `RsaSelloDigitalService` in `McpCfdi.Infrastructure/Cryptography/RsaSelloDigitalService.cs`
    - Implement `ISelloDigitalService`
    - SHA-256 hash of cadena original + RSA signature using `System.Security.Cryptography`
    - Encode result as Base64
    - Implement `ObtenerNoCertificado` (extract 20-digit serial from X.509 DER)
    - Implement `ObtenerCertificadoBase64` (Base64 encode DER certificate)
    - Throw `SigningException` for invalid key format
    - _Requirements: 10.5_

  - [x] 7.3 Write property test for cadena original format
    - **Property 8: Formato de la cadena original**
    - **Validates: Requirements 10.2, 10.3**
    - Use FsCheck `ArbComprobante` generator, verify starts/ends with `||`, no CR/TAB/consecutive spaces

  - [x] 7.4 Write property test for digital signature verification
    - **Property 12: Firma digital verificable con llave pública**
    - **Validates: Requirements 10.5**
    - Use FsCheck `Arb<string>` for cadena original, generate RSA key pair in test setup

- [x] 8. Checkpoint - Infrastructure serialization and crypto complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Implement Infrastructure - Catalog Service
  - [x] 9.1 Create `CatalogoEntry` EF Core entity and `CatalogoSatDbContext` configuration in `McpCfdi.Infrastructure/Persistence/`
    - Define table mapping for `CatalogoEntry` with index on (NombreCatalogo, Clave)
    - Add migration or configuration to existing `McpCfdiDbContext`
    - _Requirements: 11.3_

  - [x] 9.2 Create `CatalogoSatService` in `McpCfdi.Infrastructure/Catalogs/CatalogoSatService.cs`
    - Implement `ICatalogoSatService` using EF Core queries
    - `ExisteClaveAsync`: check existence with date-based vigencia filter
    - `ValidarClavesAsync`: batch validate all keys, return all failures (not fail-fast)
    - `ObtenerDecimalesMonedaAsync`: read from c_Moneda metadata JSON field
    - Throw `CatalogoUnavailableException` when DB is unreachable
    - _Requirements: 11.1, 11.2, 11.4, 11.5_

  - [x] 9.3 Write property test for catalog vigencia validation
    - **Property 11: Validación de vigencia de claves de catálogo**
    - **Validates: Requirements 11.4**
    - Use FsCheck `ArbCatalogoEntry` and `Arb<DateTime>` generators with in-memory catalog mock

- [x] 10. Implement Application Layer - Command, DTOs, Validator, and Handler
  - [x] 10.1 Create DTOs in `McpCfdi.Application/DTOs/`
    - `EmisorDto`, `ReceptorDto`, `ConceptoDto`, `TrasladoDto`, `CfdiResult`
    - _Requirements: 1.3, 2.1, 3.1, 4.2_

  - [x] 10.2 Create `GenerarCfdiCommand` in `McpCfdi.Application/Commands/GenerarCfdi/GenerarCfdiCommand.cs`
    - Record type implementing `IRequest<CfdiResult>`
    - Include all required fields: Emisor, Receptor, Conceptos, FormaPago, MetodoPago, Moneda, LugarExpedicion, Exportacion
    - Include optional: Fecha, LlavePrivadaDer, PasswordLlave, CertificadoDer
    - _Requirements: 1.3_

  - [x] 10.3 Create `GenerarCfdiCommandValidator` in `McpCfdi.Application/Commands/GenerarCfdi/GenerarCfdiCommandValidator.cs`
    - Use FluentValidation with `ICatalogoSatService` injected
    - Validate RFC structure for Emisor and Receptor
    - Validate all catalog keys via `ValidarClavesAsync` (batch, non-fail-fast)
    - Validate numeric ranges (Cantidad > 0, ValorUnitario >= 0, etc.)
    - Validate string lengths (Nombre 1-254, Descripcion 1-1000, NoIdentificacion max 100, Unidad max 20)
    - Validate Importe = Cantidad × ValorUnitario rounded to currency decimals
    - Validate at least one Concepto present
    - _Requirements: 2.2, 2.3, 2.4, 2.6, 2.7, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 11.1, 11.2_

  - [x] 10.4 Create `GenerarCfdiCommandHandler` in `McpCfdi.Application/Commands/GenerarCfdi/GenerarCfdiCommandHandler.cs`
    - Implement `IRequestHandler<GenerarCfdiCommand, CfdiResult>`
    - Orchestrate: build domain model → calculate totals → serialize → generate cadena original → sign → assign sello → return result
    - Use `ICatalogoSatService.ObtenerDecimalesMonedaAsync` for currency precision
    - _Requirements: 1.3, 7.1, 7.2, 7.3, 10.1, 10.5_

  - [x] 10.5 Write property test for batch validation reporting
    - **Property 10: Validación por lotes reporta todas las fallas**
    - **Validates: Requirements 11.1, 11.2, 3.7, 4.10**
    - Use FsCheck `ArbComprobante` with intentionally invalid catalog keys, verify all N errors reported

  - [x] 10.6 Write unit tests for `GenerarCfdiCommandHandler`
    - Test happy path: valid command produces CfdiResult with non-empty XML, cadena, sello
    - Test validation failure: invalid RFC returns appropriate error
    - Test catalog unavailable: throws `CatalogoUnavailableException`
    - _Requirements: 1.3, 1.6, 11.5_

- [x] 11. Checkpoint - Application layer complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Implement MCP Tool Exposure
  - [x] 12.1 Create `GenerarCfdiTool` MCP tool in `McpCfdi.Api/Mcp/GenerarCfdiTool.cs`
    - Decorate with `[McpServerTool]` attribute
    - Inject `ISender` (MediatR) and delegate to `GenerarCfdiCommand`
    - Map MCP error responses using `isError: true` format with complete validation error list
    - _Requirements: 1.1, 1.3_

  - [x] 12.2 Register CFDI services in DI container in `Program.cs`
    - Register `ICfdiSerializer` → `CfdiXmlSerializer`
    - Register `ICadenaOriginalGenerator` → `XsltCadenaOriginalGenerator`
    - Register `ISelloDigitalService` → `RsaSelloDigitalService`
    - Register `ICatalogoSatService` → `CatalogoSatService`
    - _Requirements: 1.1_

  - [x] 12.3 Write integration tests for MCP tool end-to-end flow
    - Test full pipeline: command → validation → domain → serialize → sign → response
    - Test error response format matches MCP error structure
    - Use SAT test certificates (CSD de pruebas)
    - _Requirements: 1.1, 1.3, 10.5_

- [x] 13. Implement FsCheck Custom Generators
  - [x] 13.1 Create FsCheck custom generators in `McpCfdi.Domain.Tests/Generators/`
    - `ArbRfc`: generates valid 12 or 13 char RFCs matching SAT pattern
    - `ArbMontoMoneda`: generates non-negative decimals with 0-6 decimal places
    - `ArbCantidad`: generates decimals > 0 with max 6 decimal places
    - `ArbConcepto`: generates valid Concepto with all fields in valid ranges
    - `ArbTrasladoConcepto`: generates valid traslado with Base > 0 and valid TipoFactor
    - `ArbComprobante`: generates arithmetically consistent full Comprobante
    - `ArbCatalogoEntry`: generates catalog entries with/without vigencia dates
    - _Requirements: All (testing infrastructure)_

- [x] 14. Final checkpoint - Full integration
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The project uses .NET 8, xUnit, FsCheck, FluentValidation, MediatR, and EF Core with PostgreSQL
- All code follows the existing Clean Architecture pattern (Domain → Application → Infrastructure → Api)
- FsCheck generators (task 13) should be implemented early enough to support property tests, but are listed last as they are a cross-cutting concern used by multiple test tasks

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.3", "1.4", "1.5", "3.1", "3.2", "3.3", "3.4", "4.1"] },
    { "id": 1, "tasks": ["1.2", "1.6", "2.1", "2.2", "2.3", "2.4", "2.5", "13.1"] },
    { "id": 2, "tasks": ["2.6", "2.7"] },
    { "id": 3, "tasks": ["2.8", "2.9", "6.1"] },
    { "id": 4, "tasks": ["6.2", "6.3", "6.4", "6.5", "6.6", "6.7", "6.8", "6.9", "6.10"] },
    { "id": 5, "tasks": ["7.1", "7.2"] },
    { "id": 6, "tasks": ["7.3", "7.4", "9.1"] },
    { "id": 7, "tasks": ["9.2"] },
    { "id": 8, "tasks": ["9.3", "10.1", "10.2"] },
    { "id": 9, "tasks": ["10.3", "10.4"] },
    { "id": 10, "tasks": ["10.5", "10.6"] },
    { "id": 11, "tasks": ["12.1", "12.2"] },
    { "id": 12, "tasks": ["12.3"] }
  ]
}
```

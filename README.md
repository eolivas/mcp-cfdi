# MCP-CFDI

An MCP (Model Context Protocol) server for generating CFDIs (Comprobante Fiscal Digital por Internet) — the electronic invoice format mandated by Mexico's tax authority (SAT).

## About the Project

This project implements an MCP server capable of generating valid CFDIs following the official **"Anexo 20 — Guía de llenado de los comprobantes fiscales digitales por Internet"** specification published by the SAT.

### What is a CFDI?

A CFDI (Comprobante Fiscal Digital por Internet) is a Digital Tax Receipt via Internet. It is the legal electronic invoice format required in Mexico since 2014 for recording taxable transactions digitally. CFDIs serve as legal proof of commercial operations and ensure compliance with Mexican tax law.

**Key characteristics:**

- **Digital Format** — Issued as XML files containing detailed transaction information: goods/services, amounts, taxes, and payment methods.
- **Legal Validation** — Each CFDI must be certified by a PAC (Proveedor Autorizado de Certificación), which digitally stamps the document and assigns a unique identifier (UUID) to guarantee authenticity.
- **Mandatory Use** — All businesses in Mexico must issue CFDIs for commercial transactions, replacing paper invoices to improve tax compliance and reduce fraud.
- **Broad Applications** — Used for commercial invoices, payroll receipts, credit notes, payment confirmations, and goods transfer documents.

### Project Goal

Build an MCP server that generates minimal, valid CFDIs including all required base information necessary for certification by a PAC. The reference document is available at [Anexo_20_Guia_de_llenado_CFDI.pdf](http://omawww.sat.gob.mx/tramitesyservicios/Paginas/documentos/Anexo_20_Guia_de_llenado_CFDI.pdf).

## Architecture Overview

A production-grade enterprise platform built with Clean Architecture, Domain-Driven Design, and CQRS/Event-Driven patterns.

This repository demonstrates how to structure a .NET 8 backend following enterprise best practices. The primary bounded context implemented is the **McpCfdi Service**.

## Tech Stack

### Backend (.NET 8)

- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → API)
- **CQRS**: MediatR for command/query separation with pipeline behaviours
- **Messaging**: MassTransit with transactional outbox pattern
- **Persistence**: Entity Framework Core + PostgreSQL (Npgsql)
- **Observability**: OpenTelemetry (tracing, metrics) + Serilog (structured logging)
- **Validation**: FluentValidation
- **Auth**: JWT Bearer authentication
- **API**: ASP.NET Core Minimal APIs + MCP (Model Context Protocol) tooling
- **Containerization**: Docker (multi-stage Dockerfile)

## Project Structure

```
McpCfdi/
├── src/
│   ├── McpCfdi.Domain/           # Aggregates, entities, value objects, domain events
│   ├── McpCfdi.Application/      # Commands, queries, handlers, DTOs, behaviours
│   ├── McpCfdi.Infrastructure/   # EF Core, MassTransit, XML/Crypto, catalogs
│   └── McpCfdi.Api/              # Minimal API endpoints, middleware, MCP tools
├── tests/
│   ├── McpCfdi.Domain.Tests/     # Domain unit & property-based tests
│   ├── McpCfdi.Application.Tests/# Handler tests
│   ├── McpCfdi.Infrastructure.Tests/
│   ├── McpCfdi.Api.Tests/
│   └── McpCfdi.Architecture.Tests/  # NetArchTest dependency rule enforcement
├── docs/
│   └── adr/                     # Architecture Decision Records
└── samples/
    └── cfdi40-sample.xml        # Sample CFDI 4.0 XML
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional, for containerized runs)

### Run Locally

```bash
cd McpCfdi
dotnet build
dotnet run --project src/McpCfdi.Api
```

### Run Tests

```bash
cd McpCfdi
dotnet test
```

### Docker

```bash
cd McpCfdi
docker build -f src/McpCfdi.Api/Dockerfile -t mcpcfdi-api .
docker run -p 8080:8080 mcpcfdi-api
```

## Architecture Decisions

Key decisions are documented as ADRs in `McpCfdi/docs/adr/`:

| ADR | Decision |
|-----|----------|
| [001](McpCfdi/docs/adr/ADR-001-clean-architecture.md) | Clean Architecture as structural foundation |
| [002](McpCfdi/docs/adr/ADR-002-mediatr-cqrs.md) | MediatR for CQRS |
| [003](McpCfdi/docs/adr/ADR-003-masstransit-messaging.md) | MassTransit for async messaging |
| [004](McpCfdi/docs/adr/ADR-004-outbox-pattern.md) | Transactional outbox for reliable event publishing |
| [005](McpCfdi/docs/adr/ADR-005-efcore-orm.md) | EF Core as ORM |

## Conventions

This project follows [Conventional Commits](https://www.conventionalcommits.org/) and standard GitHub workflow practices. See [REPO_CONVENTIONS.md](McpCfdi/docs/REPO_CONVENTIONS.md) for details on commit messages, branch naming, PR format, and code style.

## License

Private repository. All rights reserved.

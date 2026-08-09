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

### Backend (.NET 8+)

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

- [.NET 8+ SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
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

---

## Architecture Decision Records (ADRs)

ADRs document significant architectural choices and their rationale. Located in `docs/adr/`.

| ADR | Title | Summary |
|-----|-------|---------|
| [ADR-001](docs/adr/ADR-001-clean-architecture.md) | Clean Architecture | Four-layer structure with strict dependency rule enforced by NetArchTest |
| [ADR-002](docs/adr/ADR-002-mediatr-cqrs.md) | MediatR for CQRS | In-process mediator with pipeline behaviours for validation and logging |
| [ADR-003](docs/adr/ADR-003-masstransit-messaging.md) | MassTransit Messaging | Transport abstraction over RabbitMQ/SNS/Service Bus with consumer retry and DLQ |
| [ADR-004](docs/adr/ADR-004-outbox-pattern.md) | Outbox Pattern | Transactional outbox for guaranteed at-least-once event delivery |
| [ADR-005](docs/adr/ADR-005-efcore-orm.md) | EF Core ORM | Rich domain mapping with strongly-typed IDs, owned entities, and parameterized queries |
| [ADR-006](docs/adr/ADR-006-rate-limiting-security-headers.md) | Rate Limiting & Security Headers | Fixed-window rate limiter and OWASP security headers middleware |
| [ADR-007](docs/adr/ADR-007-observability-correlation.md) | Observability & Correlation | OpenTelemetry metrics/traces, local observability stack, correlation ID propagation |
| [ADR-008](docs/adr/ADR-008-microservices-architecture.md) | Microservices Architecture | Bounded context ownership, async-first communication, independent deployability |
| [ADR-009](docs/adr/ADR-009-testing-framework-selection.md) | Testing Framework Selection | xUnit + FsCheck + PBT strategy, Vitest + fast-check for frontend |
| [ADR-010](docs/adr/ADR-010-frontend-technology-selection.md) | Frontend Technology Selection | React + Vite + TanStack Query + Zustand, feature-module architecture |
| [ADR-011](docs/adr/ADR-011-manual-object-mapping.md) | Manual Object Mapping | Static `From()` methods over AutoMapper/Mapster for debuggability and compile-time safety |

---

## Steering Files Overview

Steering files provide AI-agent conventions and development guidelines. Located in `.kiro/steering/`. They supply context to Kiro so it follows the project's patterns and conventions.

### Inclusion Modes

Each steering file declares an `inclusion` mode in its YAML frontmatter that controls **when** it gets loaded into context:

| Mode | Frontmatter | Behavior | Token Impact |
|------|-------------|----------|--------------|
| **auto** | `inclusion: auto` | Loaded on every interaction | Always consumed |
| **fileMatch** | `inclusion: fileMatch` + `fileMatchPattern: "glob"` | Loaded only when a matching file is open | Conditional |
| **manual** | `inclusion: manual` | Loaded only when explicitly referenced with `#` in chat | On-demand |

**Example frontmatter:**

```yaml
---
inclusion: fileMatch
fileMatchPattern: "**/*Command*.cs,**/*Query*.cs,**/*Handler*.cs"
---
```

### Why This Matters

Loading all steering files on every interaction wastes context tokens. This project uses a tiered strategy:

- **auto** — Core conventions that apply to almost every task (architecture layers, DDD patterns, commit standards).
- **fileMatch** — Domain-specific guides loaded when you're working on relevant code (EF Core rules appear only when editing repositories, React rules only in `frontend/`).
- **manual** — Reference material for occasional deep-dives (design patterns, scaling strategies, SOLID principles). Reference them with `#12-solid-principles` in chat when needed.

### How to Use Manual Steering Files

In Kiro's chat input, type `#` followed by the steering file name to include it:

```
#25-caching-best-practices
```

This loads the file into context for that interaction only, keeping your baseline token budget lean.

### Current Configuration

| Inclusion | Files |
|-----------|-------|
| **auto** | `01` (Clean Architecture), `02` (DDD), `08` (Commits & PRs) |
| **fileMatch** | `03` (CQRS), `04` (MassTransit), `05` (Minimal API), `06` (EF Core Config), `07` (Testing), `09` (React), `10` (Docker/CI), `11` (Middleware), `16` (EF Core), `17` (Event-Driven), `20` (Logging), `21` (Configuration), `24` (Mapping) |
| **manual** | `12` (SOLID), `13` (Design Patterns), `14` (Code Review), `15` (Microservices), `18` (Arch Principles), `19` (Testing Strategy), `22` (REST API), `23` (Code Smells), `25` (Caching), `26` (Async), `27` (Security), `28` (Scaling), `29` (iSAQB) |

---

### Full Steering File Reference

| # | File | Purpose |
|---|------|---------|
| 01 | `01-clean-architecture-layer-placement.md` | Layer responsibilities, dependency rules, decision checklist |
| 02 | `02-ddd-aggregate-entity-creation.md` | Strongly-typed IDs, entity base classes, aggregate root patterns |
| 03 | `03-cqrs-command-query-scaffolding.md` | Command/query definitions, validators, handlers, DTOs, pipeline behaviours |
| 04 | `04-masstransit-consumer-event-publishing.md` | Domain events, outbox flow, consumer creation, correlation ID propagation |
| 05 | `05-minimal-api-endpoint-conventions.md` | Endpoint groups, route patterns, status codes, rate limiting, MediatR dispatch |
| 06 | `06-efcore-entity-configuration.md` | Entity type configurations, table naming, ID conversions, owned entities |
| 07 | `07-testing-conventions.md` | Test structure, naming, domain/handler/architecture/integration test patterns, PBT |
| 08 | `08-conventional-commits-pr-standards.md` | Commit format, PR template, breaking changes, diff size limits |
| 09 | `09-react-feature-module.md` | Feature directory structure, TanStack Query hooks, Zustand stores, error handling |
| 10 | `10-docker-cicd-awareness.md` | Docker Compose services, CI/CD pipeline stages, environment variables |
| 11 | `11-middleware-security-observability.md` | Middleware pipeline order, security headers, rate limiting, OpenTelemetry, CORS |
| 12 | `12-solid-principles.md` | SRP, OCP, LSP, ISP, DIP applied to each layer with examples and anti-patterns |
| 13 | `13-design-patterns.md` | Factory, Repository, Decorator, Mediator, Observer, Strategy, CQRS, Outbox |
| 14 | `14-code-review-practices.md` | Review turnaround, comment categories, .NET/React/testing/security checklists |
| 15 | `15-microservices-best-practices.md` | Service boundaries, communication patterns, resilience, data ownership, observability |
| 16 | `16-efcore-best-practices.md` | Query performance, change tracker, concurrency, migrations, connection pooling, anti-patterns |
| 17 | `17-event-driven-messaging.md` | Event design, schema versioning, idempotency, ordering, DLQ handling, saga patterns |
| 18 | `18-architectural-principles.md` | Separation of Concerns, DRY, KISS, YAGNI with examples and conflict resolution |
| 19 | `19-testing-strategy.md` | Testing pyramid, test doubles taxonomy, isolation rules, PBT strategy, frontend testing, contracts |
| 20 | `20-logging-patterns.md` | Log levels, structured templates, correlation, exception logging, what to log/avoid, configuration |
| 21 | `21-configuration-options-pattern.md` | Options pattern, validation, environment overrides, secrets management, feature flags |
| 22 | `22-restful-api-best-practices.md` | Resource naming, HTTP semantics, ProblemDetails, pagination, versioning, caching, integrations |
| 23 | `23-code-smells-antipatterns.md` | God class, feature envy, primitive obsession, anemic model, prop drilling, detection checklist |
| 24 | `24-object-mapping-conventions.md` | Manual mapping strategy, From() pattern, direction rules, null handling, anti-patterns |
| 25 | `25-caching-best-practices.md` | IMemoryCache vs. Redis, cache-aside pattern, invalidation, stampede prevention, key design |
| 26 | `26-async-patterns.md` | Async all the way, CancellationToken, Task.WhenAll, background services, pitfalls |
| 27 | `27-security-auth-patterns.md` | JWT auth, policy-based authorization, secure coding, input validation, CORS, encryption |
| 28 | `28-scaling-system-design.md` | Progressive scaling stages (0→millions), auto-scaling, sharding, performance budgets |
| 29 | `29-architecture-fundamentals-isaqb.md` | Quality attributes (ISO 25010), C4 documentation, coupling/cohesion, tech debt, ATAM |

---

## Conventions

This project follows [Conventional Commits](https://www.conventionalcommits.org/) and standard GitHub workflow practices. See [REPO_CONVENTIONS.md](McpCfdi/docs/REPO_CONVENTIONS.md) for details on commit messages, branch naming, PR format, and code style.

## License

Private repository. All rights reserved.

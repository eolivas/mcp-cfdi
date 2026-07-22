# ADR-001: Clean Architecture as Structural Foundation

## Status

Accepted

## Date

2025-01-15

## Context

We need an architecture that:

- Isolates business logic from infrastructure concerns (database, messaging, HTTP).
- Allows independent testability of each layer without requiring external services.
- Enables swapping infrastructure components (e.g., changing the database provider or message broker) without modifying domain or application logic.
- Enforces a clear dependency rule: inner layers never reference outer layers.

The CFDI generation domain is complex — it involves tax rules, SAT catalog validation, XML serialization, and cryptographic signing. Mixing these concerns into a single layer would make the system brittle and difficult to evolve.

## Decision

Adopt **Clean Architecture** (also known as Onion Architecture) with four layers:

```
McpCfdi.Domain          → Entities, Value Objects, Domain Events, Interfaces
McpCfdi.Application     → Commands, Queries, Handlers, DTOs, Pipeline Behaviours
McpCfdi.Infrastructure  → EF Core, MassTransit, HTTP Clients, XML/Crypto Services
McpCfdi.Api             → Minimal API Endpoints, MCP Tools, Middleware, Composition Root
```

**Dependency rule**: Domain ← Application ← Infrastructure ← Api. Inner layers define interfaces; outer layers provide implementations.

## Consequences

### Positive

- Domain has zero external NuGet dependencies — it is a pure .NET class library.
- Application layer depends only on Domain and lightweight abstractions (MediatR, FluentValidation, logging abstractions).
- Infrastructure implements all domain/application interfaces (repositories, event publishers, serializers).
- The Api project acts as Composition Root, wiring all services via DI.
- Architecture fitness tests (`McpCfdi.Architecture.Tests`) enforce dependency rules at build time.

### Negative

- More projects and indirection compared to a layered monolith.
- New developers need to understand the dependency rule to place code correctly.
- Mapping between layers (DTOs ↔ Domain models) adds boilerplate.

### Mitigations

- `Directory.Build.props` enforces shared settings (nullable, warnings as errors) across all projects.
- Architecture tests prevent accidental dependency violations.

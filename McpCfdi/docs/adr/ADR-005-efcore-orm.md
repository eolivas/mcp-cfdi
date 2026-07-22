# ADR-005: EF Core as ORM

## Status

Accepted

## Date

2025-01-15

## Context

The system needs to persist domain aggregates and the transactional outbox to a relational database. We need an ORM that:

- Supports rich domain model mapping (private setters, value objects, owned entities).
- Provides migration tooling for schema evolution.
- Integrates well with the .NET ecosystem and DI container.
- Supports PostgreSQL as the target database.

## Decision

Use **Entity Framework Core 8.0.11** with the **Npgsql** provider for PostgreSQL:

- `McpCfdiDbContext` is the single DbContext for the McpCfdi bounded context.
- Entity configurations use Fluent API via `IEntityTypeConfiguration<T>`, auto-discovered from the Infrastructure assembly (`ApplyConfigurationsFromAssembly`).
- The DbContext overrides `SaveChangesAsync` to implement the transactional outbox (see ADR-004).
- DbSets: `OutboxMessages`, `CatalogoEntries`.

Database connection is configured in `Program.cs`:

```csharp
builder.Services.AddDbContext<McpCfdiDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("McpCfdiDb")));
```

## Consequences

### Positive

- Mature ORM with strong LINQ support, change tracking, and migration tooling.
- Npgsql provider is production-proven for PostgreSQL.
- Fluent API configurations keep entity mappings co-located with the Infrastructure layer (Domain stays persistence-ignorant).
- OpenTelemetry instrumentation for EF Core provides query-level tracing out of the box.

### Negative

- EF Core has a learning curve for advanced scenarios (value object mapping, owned types, complex aggregates).
- Generated SQL may not be optimal for all queries — may need raw SQL or stored procedures for performance-critical paths.
- Tight coupling to relational model; switching to a document store would require significant rework.

### Mitigations

- Use `AsNoTracking()` for read-only queries.
- Profile queries using OpenTelemetry tracing and PostgreSQL query logs.
- Domain remains persistence-ignorant — only Infrastructure references EF Core.

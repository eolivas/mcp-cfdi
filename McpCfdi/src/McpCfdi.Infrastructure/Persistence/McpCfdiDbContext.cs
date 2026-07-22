using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using McpCfdi.Domain.Common;

namespace McpCfdi.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the McpCfdi bounded context.
/// Intercepts domain events during SaveChangesAsync and persists them as OutboxMessage rows
/// within the same transaction as the aggregate changes.
/// </summary>
public class McpCfdiDbContext : DbContext
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<CatalogoEntry> CatalogoEntries => Set<CatalogoEntry>();

    public McpCfdiDbContext(DbContextOptions<McpCfdiDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(McpCfdiDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events from all tracked aggregate roots before saving.
        var aggregatesWithEvents = ChangeTracker.Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // Convert each domain event to an OutboxMessage row.
        foreach (var domainEvent in domainEvents)
        {
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = domainEvent.GetType().AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredAt = domainEvent.OccurredAt,
                ProcessedAt = null
            };

            OutboxMessages.Add(outboxMessage);
        }

        // Save both aggregate changes and outbox messages in one transaction.
        var result = await base.SaveChangesAsync(cancellationToken);

        // Clear domain events after successful save.
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        return result;
    }
}

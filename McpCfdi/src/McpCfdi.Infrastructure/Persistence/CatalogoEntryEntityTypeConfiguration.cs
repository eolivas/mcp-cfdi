using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace McpCfdi.Infrastructure.Persistence;

/// <summary>
/// EF Core entity type configuration for <see cref="CatalogoEntry"/>.
/// Maps to the <c>catalogo_sat_entries</c> table with a composite index on (NombreCatalogo, Clave)
/// for efficient catalog lookups.
/// </summary>
public sealed class CatalogoEntryEntityTypeConfiguration : IEntityTypeConfiguration<CatalogoEntry>
{
    public void Configure(EntityTypeBuilder<CatalogoEntry> builder)
    {
        builder.ToTable("catalogo_sat_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NombreCatalogo)
            .IsRequired();

        builder.Property(e => e.Clave)
            .IsRequired();

        builder.Property(e => e.Descripcion)
            .IsRequired();

        builder.Property(e => e.FechaInicioVigencia)
            .IsRequired(false);

        builder.Property(e => e.FechaFinVigencia)
            .IsRequired(false);

        builder.Property(e => e.Metadata)
            .IsRequired(false);

        builder.HasIndex(e => new { e.NombreCatalogo, e.Clave });
    }
}

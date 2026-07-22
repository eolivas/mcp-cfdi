using FsCheck;
using FsCheck.Fluent;
using Microsoft.EntityFrameworkCore;
using McpCfdi.Domain.Tests.Generators;
using McpCfdi.Infrastructure.Catalogs;
using McpCfdi.Infrastructure.Persistence;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Catalogs;

/// <summary>
/// Property 11: Validación de vigencia de claves de catálogo
/// **Validates: Requirements 11.4**
/// </summary>
public class CatalogoVigenciaPropertyTests
{
    private static (McpCfdiDbContext ctx, CatalogoSatService svc) CreateInMemory(CatalogoEntry entry)
    {
        var options = new DbContextOptionsBuilder<McpCfdiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var ctx = new McpCfdiDbContext(options);
        ctx.CatalogoEntries.Add(entry);
        ctx.SaveChanges();
        var svc = new CatalogoSatService(ctx);
        return (ctx, svc);
    }

    [Fact]
    public void EntryWithinVigencia_ReturnsTrue()
    {
        // Sub-property (a): entry with FechaInicioVigencia <= fechaEmision AND FechaFinVigencia >= fechaEmision → true
        var arb = CfdiGenerators.ArbCatalogoEntry()
            .Select(entry =>
            {
                var inicio = new DateTime(2020, 1, 1);
                var fin = new DateTime(2025, 12, 31);
                entry.FechaInicioVigencia = inicio;
                entry.FechaFinVigencia = fin;
                return entry;
            }).ToArbitrary();

        var fechaArb = Gen.Choose(2020, 2025)
            .SelectMany(y => Gen.Choose(1, 12).Select(m => new DateTime(y, m, 1)))
            .ToArbitrary();

        var prop = Prop.ForAll(arb, fechaArb, (entry, fechaEmision) =>
        {
            var (ctx, svc) = CreateInMemory(entry);
            try
            {
                var result = svc.ExisteClaveAsync(entry.NombreCatalogo, entry.Clave, fechaEmision).Result;
                return result.ToProperty();
            }
            finally
            {
                ctx.Dispose();
            }
        });

        prop.QuickCheckThrowOnFailure();
    }

    [Fact]
    public void EntryNotYetValid_ReturnsFalse()
    {
        // Sub-property (b): FechaInicioVigencia > fechaEmision → false
        var arb = CfdiGenerators.ArbCatalogoEntry()
            .Select(entry =>
            {
                entry.FechaInicioVigencia = new DateTime(2025, 6, 1);
                entry.FechaFinVigencia = new DateTime(2030, 12, 31);
                return entry;
            }).ToArbitrary();

        // fechaEmision is strictly before FechaInicioVigencia
        var fechaArb = Gen.Choose(2018, 2024)
            .SelectMany(y => Gen.Choose(1, 12).Select(m => new DateTime(y, m, 1)))
            .ToArbitrary();

        var prop = Prop.ForAll(arb, fechaArb, (entry, fechaEmision) =>
        {
            var (ctx, svc) = CreateInMemory(entry);
            try
            {
                var result = svc.ExisteClaveAsync(entry.NombreCatalogo, entry.Clave, fechaEmision).Result;
                return (!result).ToProperty();
            }
            finally
            {
                ctx.Dispose();
            }
        });

        prop.QuickCheckThrowOnFailure();
    }

    [Fact]
    public void EntryExpired_ReturnsFalse()
    {
        // Sub-property (c): FechaFinVigencia < fechaEmision → false
        var arb = CfdiGenerators.ArbCatalogoEntry()
            .Select(entry =>
            {
                entry.FechaInicioVigencia = new DateTime(2015, 1, 1);
                entry.FechaFinVigencia = new DateTime(2020, 6, 1);
                return entry;
            }).ToArbitrary();

        // fechaEmision is strictly after FechaFinVigencia
        var fechaArb = Gen.Choose(2021, 2030)
            .SelectMany(y => Gen.Choose(1, 12).Select(m => new DateTime(y, m, 1)))
            .ToArbitrary();

        var prop = Prop.ForAll(arb, fechaArb, (entry, fechaEmision) =>
        {
            var (ctx, svc) = CreateInMemory(entry);
            try
            {
                var result = svc.ExisteClaveAsync(entry.NombreCatalogo, entry.Clave, fechaEmision).Result;
                return (!result).ToProperty();
            }
            finally
            {
                ctx.Dispose();
            }
        });

        prop.QuickCheckThrowOnFailure();
    }

    [Fact]
    public void EntryWithNullInicio_ValidSinceAnyPriorDate()
    {
        // Sub-property (d): FechaInicioVigencia == null → valid regardless of how old fechaEmision is
        var arb = CfdiGenerators.ArbCatalogoEntry()
            .Select(entry =>
            {
                entry.FechaInicioVigencia = null;
                entry.FechaFinVigencia = null; // Also null fin to isolate the null-inicio behavior
                return entry;
            }).ToArbitrary();

        // Generate any date, including very old dates
        var fechaArb = Gen.Choose(1990, 2030)
            .SelectMany(y => Gen.Choose(1, 12).Select(m => new DateTime(y, m, 1)))
            .ToArbitrary();

        var prop = Prop.ForAll(arb, fechaArb, (entry, fechaEmision) =>
        {
            var (ctx, svc) = CreateInMemory(entry);
            try
            {
                var result = svc.ExisteClaveAsync(entry.NombreCatalogo, entry.Clave, fechaEmision).Result;
                return result.ToProperty();
            }
            finally
            {
                ctx.Dispose();
            }
        });

        prop.QuickCheckThrowOnFailure();
    }

    [Fact]
    public void EntryWithNullFin_ValidIndefinitely()
    {
        // Sub-property (e): FechaFinVigencia == null → valid indefinitely
        var arb = CfdiGenerators.ArbCatalogoEntry()
            .Select(entry =>
            {
                entry.FechaInicioVigencia = new DateTime(2020, 1, 1);
                entry.FechaFinVigencia = null;
                return entry;
            }).ToArbitrary();

        // Generate dates on or after FechaInicioVigencia, including far future
        var fechaArb = Gen.Choose(2020, 2099)
            .SelectMany(y => Gen.Choose(1, 12).Select(m => new DateTime(y, m, 1)))
            .ToArbitrary();

        var prop = Prop.ForAll(arb, fechaArb, (entry, fechaEmision) =>
        {
            var (ctx, svc) = CreateInMemory(entry);
            try
            {
                var result = svc.ExisteClaveAsync(entry.NombreCatalogo, entry.Clave, fechaEmision).Result;
                return result.ToProperty();
            }
            finally
            {
                ctx.Dispose();
            }
        });

        prop.QuickCheckThrowOnFailure();
    }
}

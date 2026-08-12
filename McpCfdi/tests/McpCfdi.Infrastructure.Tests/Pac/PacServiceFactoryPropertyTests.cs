using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Application.Interfaces;
using McpCfdi.Infrastructure.Pac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Pac;

/// <summary>
/// Property 6: Cambio de PAC por configuración
/// **Validates: Requirements 4.2, 11.2**
///
/// Para cualquier valor válido de ActiveProvider ("Multifacturas", "FiscalCloud"),
/// el factory resuelve la implementación correspondiente.
/// Para valor inválido, lanza InvalidOperationException.
/// </summary>
public class PacServiceFactoryPropertyTests
{
    private static readonly string[] ValidProviders = ["Multifacturas", "FiscalCloud"];

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // MultifacturasPacAdapter requires HttpClient, IOptions<PacOptions>, ILogger<MultifacturasPacAdapter>
        var pacOptions = Options.Create(new PacOptions
        {
            ActiveProvider = "Multifacturas",
            Multifacturas = new MultifacturasPacOptions
            {
                BaseUrl = "https://fake.local",
                ApiKey = "test-key"
            }
        });
        services.AddSingleton<IOptions<PacOptions>>(pacOptions);
        services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri("https://fake.local") });
        services.AddSingleton<MultifacturasPacAdapter>();
        services.AddSingleton<FiscalCloudPacAdapter>();
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static PacServiceFactory CreateFactory(string activeProvider, IServiceProvider serviceProvider)
    {
        var options = Options.Create(new PacOptions { ActiveProvider = activeProvider });
        return new PacServiceFactory(serviceProvider, options);
    }

    /// <summary>
    /// **Validates: Requirements 4.2, 11.2**
    /// Property 6a: For any valid ActiveProvider value, the factory creates a non-null
    /// IPacService wrapped as PacResilienceDecorator.
    /// </summary>
    [Fact]
    public void Create_WithAnyValidProvider_ReturnsNonNullPacResilienceDecorator()
    {
        var gen = Gen.Elements(ValidProviders).ToArbitrary();

        var prop = Prop.ForAll(gen, provider =>
        {
            using var sp = BuildServiceProvider();
            var factory = CreateFactory(provider, sp);

            var result = factory.Create();

            return (result is not null && result is PacResilienceDecorator).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// **Validates: Requirements 4.2, 11.2**
    /// Property 6b: For any arbitrary non-empty string that is NOT a valid provider,
    /// the factory throws InvalidOperationException with a message referencing
    /// the invalid provider and valid options.
    /// </summary>
    [Fact]
    public void Create_WithInvalidProvider_ThrowsInvalidOperationException()
    {
        var gen = ArbMap.Default.GeneratorFor<NonEmptyString>()
            .Where(s => !ValidProviders.Contains(s.Item))
            .ToArbitrary();

        var prop = Prop.ForAll(gen, nonEmptyStr =>
        {
            var invalidProvider = nonEmptyStr.Item;
            using var sp = BuildServiceProvider();
            var factory = CreateFactory(invalidProvider, sp);

            try
            {
                factory.Create();
                return false.ToProperty();
            }
            catch (InvalidOperationException ex)
            {
                return (ex.Message.Contains(invalidProvider) &&
                        ex.Message.Contains("Multifacturas") &&
                        ex.Message.Contains("FiscalCloud")).ToProperty();
            }
        });

        prop.QuickCheckThrowOnFailure();
    }
}

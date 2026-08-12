using McpCfdi.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpCfdi.Infrastructure.Pac;

/// <summary>
/// Métodos de extensión para registrar los servicios PAC en el contenedor de DI.
/// </summary>
public static class PacDependencyInjection
{
    /// <summary>
    /// Registra todos los servicios necesarios para la integración con PAC:
    /// opciones, proveedores de credenciales, HttpClients tipados y la factory de IPacService.
    /// </summary>
    public static IServiceCollection AddPacServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration sections
        services.Configure<PacOptions>(configuration.GetSection(PacOptions.SectionName));
        services.Configure<EmisoresOptions>(configuration.GetSection(EmisoresOptions.SectionName));

        // IHttpContextAccessor para propagación de correlation ID en llamadas al PAC
        services.AddHttpContextAccessor();

        // Proveedor de credenciales del emisor (carga .cer/.key de disco, password de env)
        services.AddSingleton<IEmisorCredencialesProvider, FileSystemEmisorCredencialesProvider>();

        // Registro del HttpClient para Multifacturas con configuración desde opciones
        services.AddHttpClient<MultifacturasPacAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PacOptions>>().Value.Multifacturas;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });

        // Registro del HttpClient para FiscalCloud (futuro)
        services.AddHttpClient<FiscalCloudPacAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PacOptions>>().Value.FiscalCloud;
            if (options is null) return;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });

        // Factory + IPacService resolution
        services.AddSingleton<PacServiceFactory>();
        services.AddSingleton<IPacService>(sp => sp.GetRequiredService<PacServiceFactory>().Create());

        return services;
    }
}

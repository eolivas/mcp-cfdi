using McpCfdi.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpCfdi.Infrastructure.Pac;

/// <summary>
/// Resuelve la implementación de IPacService según la configuración ActiveProvider.
/// Registrado como Singleton — el PAC activo se determina al iniciar la aplicación.
/// </summary>
public class PacServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PacOptions _options;

    public PacServiceFactory(IServiceProvider serviceProvider, IOptions<PacOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    /// <summary>
    /// Crea una instancia de IPacService según el ActiveProvider configurado,
    /// envuelta con el decorator de resiliencia.
    /// </summary>
    /// <returns>IPacService con políticas de resiliencia aplicadas.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza cuando el ActiveProvider no corresponde a un proveedor registrado.
    /// </exception>
    public IPacService Create()
    {
        IPacService adapter = _options.ActiveProvider switch
        {
            "Multifacturas" => _serviceProvider.GetRequiredService<MultifacturasPacAdapter>(),
            "FiscalCloud" => _serviceProvider.GetRequiredService<FiscalCloudPacAdapter>(),
            _ => throw new InvalidOperationException(
                $"PAC provider '{_options.ActiveProvider}' no está registrado. " +
                $"Valores válidos: Multifacturas, FiscalCloud.")
        };

        // Wraps with resilience decorator
        return new PacResilienceDecorator(
            adapter,
            _serviceProvider.GetRequiredService<ILogger<PacResilienceDecorator>>());
    }
}

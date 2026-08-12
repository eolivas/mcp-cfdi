using System.Diagnostics;
using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using McpCfdi.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace McpCfdi.Infrastructure.Pac;

/// <summary>
/// Decorator que envuelve cualquier IPacService con retry y circuit breaker.
/// No conoce la implementación concreta del PAC — solo agrega resiliencia.
/// 
/// Retry: backoff exponencial (3 reintentos) solo para PacTransientException.
/// Circuit breaker: abre tras 5 fallos consecutivos de PacTransientException, cierra tras 30s.
/// Errores no transitorios (PacValidationException, PacAuthenticationException, etc.)
/// se propagan inmediatamente sin retry.
/// 
/// Observabilidad: cada operación registra métricas de latencia y resultado (éxito/fallo)
/// con structured logging. No se registran credenciales, llaves privadas ni certificados.
/// </summary>
public class PacResilienceDecorator : IPacService
{
    private readonly IPacService _inner;
    private readonly ILogger<PacResilienceDecorator> _logger;
    private readonly ResiliencePipeline _pipeline;

    /// <summary>
    /// Internal constructor for testability — allows injecting a custom pipeline with zero delays.
    /// </summary>
    internal PacResilienceDecorator(IPacService inner, ILogger<PacResilienceDecorator> logger, ResiliencePipeline pipeline)
    {
        _inner = inner;
        _logger = logger;
        _pipeline = pipeline;
    }

    public PacResilienceDecorator(IPacService inner, ILogger<PacResilienceDecorator> logger)
    {
        _inner = inner;
        _logger = logger;

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<PacTransientException>(),
                MaxRetryAttempts = 3,
                DelayGenerator = args =>
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber + 1));
                    return ValueTask.FromResult<TimeSpan?>(delay);
                },
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "PAC retry {Attempt}/3 after {Delay}s",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<PacTransientException>(),
                FailureRatio = 1.0,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(60),
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogError(
                        args.Outcome.Exception,
                        "PAC circuit breaker OPEN for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("PAC circuit breaker CLOSED");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<TimbradoResult> TimbrarAsync(string cfdiXmlSellado, CancellationToken ct = default)
    {
        return await ExecuteWithResilienceAsync(
            nameof(TimbrarAsync), () => _inner.TimbrarAsync(cfdiXmlSellado, ct), ct);
    }

    public async Task<CancelacionResult> CancelarAsync(CancelacionRequest request, CancellationToken ct = default)
    {
        return await ExecuteWithResilienceAsync(
            nameof(CancelarAsync), () => _inner.CancelarAsync(request, ct), ct);
    }

    public async Task<EstatusCfdiResult> ConsultarEstatusAsync(ConsultaEstatusRequest request, CancellationToken ct = default)
    {
        return await ExecuteWithResilienceAsync(
            nameof(ConsultarEstatusAsync), () => _inner.ConsultarEstatusAsync(request, ct), ct);
    }

    private async Task<T> ExecuteWithResilienceAsync<T>(string operationName, Func<Task<T>> action, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _pipeline.ExecuteAsync(
                async token => await action(), ct);

            stopwatch.Stop();

            _logger.LogInformation(
                "PAC operation {Operation} completed successfully in {LatencyMs}ms",
                operationName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (BrokenCircuitException ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                "PAC operation {Operation} failed in {LatencyMs}ms — circuit breaker open. Error: {ErrorType}",
                operationName,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name);

            throw new PacUnavailableException(
                "El PAC no está disponible — circuit breaker abierto tras fallos consecutivos.",
                ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                "PAC operation {Operation} failed in {LatencyMs}ms. ErrorType: {ErrorType}, Message: {ErrorMessage}",
                operationName,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name,
                ex.Message);

            throw;
        }
    }
}

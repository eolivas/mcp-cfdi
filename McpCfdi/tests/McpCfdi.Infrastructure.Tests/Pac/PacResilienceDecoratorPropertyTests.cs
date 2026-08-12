using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using McpCfdi.Application.DTOs;
using McpCfdi.Application.Interfaces;
using McpCfdi.Infrastructure.Exceptions;
using McpCfdi.Infrastructure.Pac;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Pac;

/// <summary>
/// Property 4: Retry solo en errores transitorios
/// **Validates: Requirements 8.1, 8.2**
///
/// Para cualquier PacTransientException, el decorator reintenta hasta 3 veces (4 llamadas totales).
/// Para cualquier PacValidationException, propaga inmediatamente sin retry (1 llamada total).
///
/// Property 5: Circuit breaker se abre tras 5 fallos consecutivos
/// **Validates: Requirements 8.1, 8.2**
///
/// Para secuencias de 5+ PacTransientException consecutivas, las llamadas posteriores lanzan
/// PacUnavailableException sin contactar al inner service.
/// </summary>
public class PacResilienceDecoratorPropertyTests
{
    private static readonly NullLogger<PacResilienceDecorator> Logger = new();

    /// <summary>
    /// Fake IPacService that counts calls and throws a configurable exception.
    /// </summary>
    private sealed class FakePacService : IPacService
    {
        private int _callCount;
        private readonly Func<Exception> _exceptionFactory;

        public int CallCount => _callCount;

        public FakePacService(Func<Exception> exceptionFactory)
        {
            _exceptionFactory = exceptionFactory;
        }

        public Task<TimbradoResult> TimbrarAsync(string cfdiXmlSellado, CancellationToken ct = default)
        {
            Interlocked.Increment(ref _callCount);
            throw _exceptionFactory();
        }

        public Task<CancelacionResult> CancelarAsync(CancelacionRequest request, CancellationToken ct = default)
        {
            Interlocked.Increment(ref _callCount);
            throw _exceptionFactory();
        }

        public Task<EstatusCfdiResult> ConsultarEstatusAsync(ConsultaEstatusRequest request, CancellationToken ct = default)
        {
            Interlocked.Increment(ref _callCount);
            throw _exceptionFactory();
        }
    }

    /// <summary>
    /// Builds a pipeline with only retry (no circuit breaker) for Property 4 isolation.
    /// Zero delays for fast tests.
    /// </summary>
    private static ResiliencePipeline BuildRetryOnlyPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<PacTransientException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.Zero,
                BackoffType = DelayBackoffType.Constant
            })
            .Build();
    }

    /// <summary>
    /// Builds a pipeline with only circuit breaker (no retry) for Property 5 isolation.
    /// </summary>
    private static ResiliencePipeline BuildCircuitBreakerOnlyPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<PacTransientException>(),
                FailureRatio = 1.0,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(60),
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .Build();
    }

    #region Property 4: Retry solo en errores transitorios

    /// <summary>
    /// **Validates: Requirements 8.1, 8.2**
    /// For any PacTransientException with arbitrary message, the decorator retries up to 3 times
    /// (total 4 calls to inner service: 1 initial + 3 retries).
    /// </summary>
    [Fact]
    public void TransientException_RetriesUpTo3Times_TotalOf4Calls()
    {
        var gen = ArbMap.Default.GeneratorFor<NonEmptyString>().ToArbitrary();

        var prop = Prop.ForAll(gen, errorMessage =>
        {
            var fake = new FakePacService(() => new PacTransientException(errorMessage.Item));
            var pipeline = BuildRetryOnlyPipeline();
            var decorator = new PacResilienceDecorator(fake, Logger, pipeline);

            try
            {
                decorator.TimbrarAsync("<xml/>", CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (PacTransientException)
            {
                // Expected — all retries exhausted
            }

            // 1 initial + 3 retries = 4 total calls
            return (fake.CallCount == 4).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// **Validates: Requirements 8.1, 8.2**
    /// For any PacValidationException with arbitrary message, the decorator does NOT retry —
    /// inner service is called exactly once.
    /// </summary>
    [Fact]
    public void ValidationException_PropagatesImmediately_NoRetry()
    {
        var gen = ArbMap.Default.GeneratorFor<NonEmptyString>().ToArbitrary();

        var prop = Prop.ForAll(gen, errorMessage =>
        {
            var fake = new FakePacService(() => new PacValidationException(errorMessage.Item));
            var pipeline = BuildRetryOnlyPipeline();
            var decorator = new PacResilienceDecorator(fake, Logger, pipeline);

            try
            {
                decorator.TimbrarAsync("<xml/>", CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (PacValidationException)
            {
                // Expected — propagated immediately
            }

            // No retry — exactly 1 call
            return (fake.CallCount == 1).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    #endregion

    #region Property 5: Circuit breaker se abre tras 5 fallos consecutivos

    /// <summary>
    /// **Validates: Requirements 8.1, 8.2**
    /// After 5 consecutive PacTransientException failures, subsequent calls throw
    /// PacUnavailableException WITHOUT calling the inner service.
    /// </summary>
    [Fact]
    public void CircuitBreaker_OpensAfter5ConsecutiveTransientFailures()
    {
        var gen = ArbMap.Default.GeneratorFor<NonEmptyString>().ToArbitrary();

        var prop = Prop.ForAll(gen, errorMessage =>
        {
            var fake = new FakePacService(() => new PacTransientException(errorMessage.Item));
            var pipeline = BuildCircuitBreakerOnlyPipeline();
            var decorator = new PacResilienceDecorator(fake, Logger, pipeline);

            // Fire 5 calls that all throw PacTransientException — these open the circuit
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    decorator.TimbrarAsync("<xml/>", CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (PacTransientException)
                {
                    // Expected
                }
                catch (PacUnavailableException)
                {
                    // May happen if circuit opens mid-sequence
                }
            }

            var callCountAfterOpening = fake.CallCount;

            // 6th call should throw PacUnavailableException without hitting inner service
            var threwUnavailable = false;
            try
            {
                decorator.TimbrarAsync("<xml/>", CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (PacUnavailableException)
            {
                threwUnavailable = true;
            }
            catch (PacTransientException)
            {
                // Circuit may not have opened yet (edge case)
            }

            var innerNotCalled = fake.CallCount == callCountAfterOpening;

            // The circuit breaker should be open: PacUnavailableException thrown
            // AND the inner service should NOT have been called again
            return (threwUnavailable && innerNotCalled).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }

    #endregion
}

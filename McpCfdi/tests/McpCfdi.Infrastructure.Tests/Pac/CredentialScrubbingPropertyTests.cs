using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Application.DTOs;
using McpCfdi.Infrastructure.Pac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Pac;

/// <summary>
/// Property 7: Credenciales no aparecen en logs
/// **Validates: Requirements 10.2**
///
/// Para cualquier llamada que involucre LlavePrivadaBase64, PasswordLlave o CertificadoBase64,
/// estos valores NO aparecen en ningún mensaje de log.
/// </summary>
public class CredentialScrubbingPropertyTests
{
    /// <summary>
    /// Prefix used to make generated credentials unique and unlikely to match substrings of log messages.
    /// Real credentials (Base64 cert/key, passwords) are always long strings, so this models reality.
    /// </summary>
    private const string CredentialPrefix = "CRED_SECRET_";

    /// <summary>
    /// Logger that captures all formatted log messages for inspection.
    /// </summary>
    private sealed class CapturingLogger : ILogger<MultifacturasPacAdapter>
    {
        private readonly List<string> _messages = new();

        public IReadOnlyList<string> Messages => _messages;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            _messages.Add(message);
        }
    }

    /// <summary>
    /// DelegatingHandler that returns a successful cancelación response without making real HTTP calls.
    /// </summary>
    private sealed class FakeSuccessHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var responseBody = new
            {
                uuid = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
                estatusUuid = "201",
                acuseXml = "<acuse/>",
                fechaCancelacion = "2024-01-15T10:30:00"
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(responseBody),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// **Validates: Requirements 10.2**
    /// For any arbitrary credential strings (CertificadoBase64, LlavePrivadaBase64, PasswordLlave),
    /// after calling CancelarAsync, NONE of the captured log messages contain any of those credential values.
    /// Credentials are prefixed to model realistic long values that won't coincidentally match log text.
    /// </summary>
    [Fact]
    public void Credentials_NeverAppearInLogMessages()
    {
        var genNonEmpty = ArbMap.Default.GeneratorFor<NonEmptyString>().ToArbitrary();

        var prop = Prop.ForAll(genNonEmpty, genNonEmpty, genNonEmpty, (certB64, keyB64, password) =>
        {
            // Prefix generated values to simulate realistic credential lengths and avoid
            // false positives from single-character strings matching log boilerplate.
            var certValue = CredentialPrefix + certB64.Item;
            var keyValue = CredentialPrefix + keyB64.Item;
            var passwordValue = CredentialPrefix + password.Item;

            var capturingLogger = new CapturingLogger();
            var options = Options.Create(new PacOptions
            {
                Multifacturas = new MultifacturasPacOptions { BaseUrl = "http://localhost" }
            });

            using var handler = new FakeSuccessHandler();
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };

            var adapter = new MultifacturasPacAdapter(httpClient, options, capturingLogger);

            var request = new CancelacionRequest(
                Uuid: "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
                RfcEmisor: "XAXX010101000",
                Motivo: "02",
                UuidSustitucion: null,
                CertificadoBase64: certValue,
                LlavePrivadaBase64: keyValue,
                PasswordLlave: passwordValue);

            adapter.CancelarAsync(request, CancellationToken.None).GetAwaiter().GetResult();

            // Assert: none of the credential values appear in any log message
            var allMessages = string.Join(Environment.NewLine, capturingLogger.Messages);

            var certNotInLogs = !allMessages.Contains(certValue, StringComparison.Ordinal);
            var keyNotInLogs = !allMessages.Contains(keyValue, StringComparison.Ordinal);
            var passwordNotInLogs = !allMessages.Contains(passwordValue, StringComparison.Ordinal);

            return (certNotInLogs && keyNotInLogs && passwordNotInLogs).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

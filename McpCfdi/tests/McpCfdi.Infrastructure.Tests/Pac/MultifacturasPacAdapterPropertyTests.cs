using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Infrastructure.Pac;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Pac;

/// <summary>
/// Property 1: Respuesta de timbrado contiene TimbreFiscalDigital
/// **Validates: Requirements 1.2, 1.3**
///
/// Para cualquier respuesta exitosa del adaptador, el CfdiTimbradoXml contiene nodo
/// tfd:TimbreFiscalDigital con atributos UUID, FechaTimbrado, SelloCFD, NoCertificadoSAT,
/// SelloSAT y Version="1.1" todos no vacíos.
/// </summary>
public class MultifacturasPacAdapterPropertyTests
{
    private const string TfdNamespace = "http://www.sat.gob.mx/TimbreFiscalDigital";

    private static readonly NullLogger<MultifacturasPacAdapter> Logger = new();

    /// <summary>
    /// Fake HttpMessageHandler that returns a predetermined JSON response.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public FakeHttpMessageHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Builds a TimbreFiscalDigital XML string with the given attributes.
    /// </summary>
    private static string BuildTimbradoXml(
        string uuid,
        string fechaTimbrado,
        string selloCfd,
        string noCertificadoSat,
        string selloSat)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <cfdi:Comprobante xmlns:cfdi="http://www.sat.gob.mx/cfd/4" xmlns:tfd="http://www.sat.gob.mx/TimbreFiscalDigital">
              <cfdi:Complemento>
                <tfd:TimbreFiscalDigital xmlns:tfd="{TfdNamespace}" Version="1.1" UUID="{uuid}" FechaTimbrado="{fechaTimbrado}" SelloCFD="{selloCfd}" NoCertificadoSAT="{noCertificadoSat}" SelloSAT="{selloSat}" />
              </cfdi:Complemento>
            </cfdi:Comprobante>
            """;
    }

    /// <summary>
    /// Builds the JSON response that the fake PAC API returns.
    /// </summary>
    private static string BuildResponseJson(
        string uuid,
        DateTime fechaTimbrado,
        string selloCfd,
        string noCertificadoSat,
        string selloSat,
        string cfdiTimbradoXml)
    {
        var responseObj = new
        {
            uuid,
            fechaTimbrado,
            selloSat,
            noCertificadoSat,
            selloCfd,
            cfdiTimbradoXml
        };
        return JsonSerializer.Serialize(responseObj);
    }

    private static MultifacturasPacAdapter CreateAdapter(string responseJson)
    {
        var handler = new FakeHttpMessageHandler(responseJson);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://fake-pac.local")
        };

        var options = Options.Create(new PacOptions
        {
            ActiveProvider = "Multifacturas",
            Multifacturas = new MultifacturasPacOptions
            {
                BaseUrl = "https://fake-pac.local",
                ApiKey = "test-key",
                Usuario = "test-user",
                Password = "test-pass"
            }
        });

        return new MultifacturasPacAdapter(httpClient, options, Logger);
    }

    /// <summary>
    /// Record to hold generated timbrado data for the property test.
    /// </summary>
    private sealed record TimbradoData(
        string Uuid,
        string SelloCfd,
        string SelloSat,
        string NoCertificadoSat,
        DateTime FechaTimbrado);

    private static Arbitrary<TimbradoData> TimbradoDataArbitrary()
    {
        var alphaNumGen = Gen.Elements(
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray())
            .ArrayOf()
            .Where(arr => arr.Length > 0)
            .Select(arr => new string(arr));

        var numericGen = Gen.Elements("0123456789".ToCharArray())
            .ArrayOf(20)
            .Select(arr => new string(arr));

        var dateGen = ArbMap.Default.GeneratorFor<DateTime>()
            .Where(d => d.Year > 2000 && d.Year < 2100);

        var gen = from selloCfd in alphaNumGen
                  from selloSat in alphaNumGen
                  from noCert in numericGen
                  from fecha in dateGen
                  select new TimbradoData(
                      Guid.NewGuid().ToString(),
                      selloCfd,
                      selloSat,
                      noCert,
                      fecha);

        return gen.ToArbitrary();
    }

    /// <summary>
    /// **Validates: Requirements 1.2, 1.3**
    ///
    /// Property 1: For any successful timbrado response, the CfdiTimbradoXml contains a
    /// tfd:TimbreFiscalDigital element with all required SAT attributes (UUID, FechaTimbrado,
    /// SelloCFD, NoCertificadoSAT, SelloSAT) non-empty and Version="1.1".
    /// </summary>
    [Fact]
    public void TimbradoResponse_ContainsTimbreFiscalDigital_WithAllRequiredAttributes()
    {
        var arb = TimbradoDataArbitrary();

        var prop = Prop.ForAll(arb, data =>
        {
            var fechaStr = data.FechaTimbrado.ToString("yyyy-MM-ddTHH:mm:ss");
            var xml = BuildTimbradoXml(data.Uuid, fechaStr, data.SelloCfd, data.NoCertificadoSat, data.SelloSat);
            var json = BuildResponseJson(data.Uuid, data.FechaTimbrado, data.SelloCfd, data.NoCertificadoSat, data.SelloSat, xml);

            var adapter = CreateAdapter(json);
            var result = adapter.TimbrarAsync("<cfdi/>", CancellationToken.None).GetAwaiter().GetResult();

            // Parse the returned CfdiTimbradoXml
            var doc = XDocument.Parse(result.CfdiTimbradoXml);
            XNamespace tfd = TfdNamespace;
            var timbre = doc.Descendants(tfd + "TimbreFiscalDigital").FirstOrDefault();

            if (timbre is null)
                return false.ToProperty();

            var attrUuid = timbre.Attribute("UUID")?.Value;
            var attrFecha = timbre.Attribute("FechaTimbrado")?.Value;
            var attrSelloCfd = timbre.Attribute("SelloCFD")?.Value;
            var attrNoCert = timbre.Attribute("NoCertificadoSAT")?.Value;
            var attrSelloSat = timbre.Attribute("SelloSAT")?.Value;
            var attrVersion = timbre.Attribute("Version")?.Value;

            var allNonEmpty =
                !string.IsNullOrEmpty(attrUuid) &&
                !string.IsNullOrEmpty(attrFecha) &&
                !string.IsNullOrEmpty(attrSelloCfd) &&
                !string.IsNullOrEmpty(attrNoCert) &&
                !string.IsNullOrEmpty(attrSelloSat);

            var versionCorrect = attrVersion == "1.1";

            return (allNonEmpty && versionCorrect).ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

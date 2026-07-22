using System.Security.Cryptography;
using System.Text;
using FsCheck;
using FsCheck.Fluent;
using McpCfdi.Infrastructure.Cryptography;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Cryptography;

/// <summary>
/// Property 12: Firma digital verificable con llave pública
/// **Validates: Requirements 10.5**
///
/// Para cualquier cadena original y para cualquier par de llaves RSA válido (privada/pública),
/// el sello digital producido por Firmar DEBERÁ ser verificable positivamente usando el
/// algoritmo SHA-256 con RSA y la llave pública correspondiente.
/// </summary>
public class DigitalSignaturePropertyTests
{
    [Fact]
    public void Firmar_ProducesVerifiableSignature_WithCorrespondingPublicKey()
    {
        var service = new RsaSelloDigitalService();

        // Generate an RSA key pair for testing (once, outside the property loop for performance)
        using var rsa = RSA.Create(2048);
        var privateKeyDer = rsa.ExportPkcs8PrivateKey();
        var publicKey = rsa.ExportSubjectPublicKeyInfo();

        // Use FsCheck to generate arbitrary non-empty strings for cadena original
        var arb = ArbMap.Default.GeneratorFor<NonEmptyString>().ToArbitrary();

        var prop = Prop.ForAll(arb, cadenaWrapper =>
        {
            var cadenaOriginal = cadenaWrapper.Item;

            // Sign using the service
            var selloBase64 = service.Firmar(cadenaOriginal, privateKeyDer, null);
            var selloBytes = Convert.FromBase64String(selloBase64);

            // Verify using the public key
            using var rsaVerify = RSA.Create();
            rsaVerify.ImportSubjectPublicKeyInfo(publicKey, out _);

            var cadenaBytes = Encoding.UTF8.GetBytes(cadenaOriginal);
            var isValid = rsaVerify.VerifyData(cadenaBytes, selloBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return isValid.ToProperty();
        });

        prop.QuickCheckThrowOnFailure();
    }
}

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using McpCfdi.Domain.Interfaces;
using McpCfdi.Infrastructure.Exceptions;

namespace McpCfdi.Infrastructure.Cryptography;

/// <summary>
/// RSA-based implementation of <see cref="ISelloDigitalService"/> for signing CFDI documents
/// using SAT-issued private keys and certificates.
/// </summary>
public class RsaSelloDigitalService : ISelloDigitalService
{
    /// <inheritdoc />
    public string Firmar(string cadenaOriginal, byte[] llavePrivadaDer, string? passwordLlave)
    {
        try
        {
            using var rsa = RSA.Create();

            if (passwordLlave is not null)
            {
                rsa.ImportEncryptedPkcs8PrivateKey(
                    Encoding.UTF8.GetBytes(passwordLlave),
                    llavePrivadaDer,
                    out _);
            }
            else
            {
                rsa.ImportPkcs8PrivateKey(llavePrivadaDer, out _);
            }

            var cadenaBytes = Encoding.UTF8.GetBytes(cadenaOriginal);
            var signature = rsa.SignData(cadenaBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signature);
        }
        catch (CryptographicException ex)
        {
            throw new SigningException("Failed to sign cadena original. The private key format is invalid or the password is incorrect.", ex);
        }
    }

    /// <inheritdoc />
    public string ObtenerNoCertificado(byte[] certificadoDer)
    {
        try
        {
            using var cert = new X509Certificate2(certificadoDer);

            // SAT certificates encode the 20-digit serial number as ASCII bytes.
            // X509Certificate2.GetSerialNumber() returns the bytes in little-endian order,
            // so we reverse them to get big-endian (the natural reading order).
            var serialBytes = cert.GetSerialNumber();
            Array.Reverse(serialBytes);

            return Encoding.ASCII.GetString(serialBytes);
        }
        catch (CryptographicException ex)
        {
            throw new SigningException("Failed to extract certificate serial number. The certificate format is invalid.", ex);
        }
    }

    /// <inheritdoc />
    public string ObtenerCertificadoBase64(byte[] certificadoDer)
    {
        return Convert.ToBase64String(certificadoDer);
    }
}

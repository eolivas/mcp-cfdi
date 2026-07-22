namespace McpCfdi.Domain.Interfaces;

/// <summary>
/// Port for digital seal (sello) operations: signing with the private key
/// and extracting certificate information.
/// </summary>
public interface ISelloDigitalService
{
    /// <summary>
    /// Signs the cadena original using SHA-256 with RSA and returns the Base64-encoded signature.
    /// </summary>
    /// <param name="cadenaOriginal">The cadena original to sign.</param>
    /// <param name="llavePrivadaDer">The private key in DER format.</param>
    /// <param name="passwordLlave">Optional password for the private key (null if unprotected).</param>
    /// <returns>The Base64-encoded digital seal.</returns>
    string Firmar(string cadenaOriginal, byte[] llavePrivadaDer, string? passwordLlave);

    /// <summary>
    /// Extracts the 20-digit certificate number from a DER-encoded certificate.
    /// </summary>
    /// <param name="certificadoDer">The certificate in DER format.</param>
    /// <returns>The 20-digit certificate serial number.</returns>
    string ObtenerNoCertificado(byte[] certificadoDer);

    /// <summary>
    /// Encodes a DER-encoded certificate to Base64 for inclusion in the CFDI XML.
    /// </summary>
    /// <param name="certificadoDer">The certificate in DER format.</param>
    /// <returns>The Base64-encoded certificate.</returns>
    string ObtenerCertificadoBase64(byte[] certificadoDer);
}

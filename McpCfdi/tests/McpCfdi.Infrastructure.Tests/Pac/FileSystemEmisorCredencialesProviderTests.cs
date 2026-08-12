using McpCfdi.Infrastructure.Exceptions;
using McpCfdi.Infrastructure.Pac;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpCfdi.Infrastructure.Tests.Pac;

/// <summary>
/// Unit tests for FileSystemEmisorCredencialesProvider.
/// Uses real temporary directories and environment variables.
/// **Validates: Requirements 6.4**
/// </summary>
public class FileSystemEmisorCredencialesProviderTests : IDisposable
{
    private const string TestRfc = "TEST000000XX1";
    private const string EnvVarName = $"EMISOR__{TestRfc}__PASSWORD_LLAVE";

    private readonly string _tempDir;
    private readonly FileSystemEmisorCredencialesProvider _sut;

    public FileSystemEmisorCredencialesProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"McpCfdiTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var options = Options.Create(new EmisoresOptions { CertificadosDir = _tempDir });
        var logger = NullLogger<FileSystemEmisorCredencialesProvider>.Instance;
        _sut = new FileSystemEmisorCredencialesProvider(options, logger);
    }

    public void Dispose()
    {
        // Clean up environment variable
        Environment.SetEnvironmentVariable(EnvVarName, null);

        // Clean up temp directory
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private void CreateCertificadoFile(byte[]? content = null)
    {
        var rfcDir = Path.Combine(_tempDir, TestRfc);
        Directory.CreateDirectory(rfcDir);
        File.WriteAllBytes(Path.Combine(rfcDir, "certificado.cer"), content ?? [0x01, 0x02, 0x03]);
    }

    private void CreateLlaveFile(byte[]? content = null)
    {
        var rfcDir = Path.Combine(_tempDir, TestRfc);
        Directory.CreateDirectory(rfcDir);
        File.WriteAllBytes(Path.Combine(rfcDir, "llave.key"), content ?? [0x04, 0x05, 0x06]);
    }

    [Fact]
    public async Task ObtenerCredencialesAsync_ConArchivosExistentes_RetornaCredencialesCorrectas()
    {
        // Arrange
        var cerBytes = new byte[] { 0xAA, 0xBB, 0xCC };
        var keyBytes = new byte[] { 0xDD, 0xEE, 0xFF };
        var password = "MiPassword123";

        CreateCertificadoFile(cerBytes);
        CreateLlaveFile(keyBytes);
        Environment.SetEnvironmentVariable(EnvVarName, password);

        // Act
        var credenciales = await _sut.ObtenerCredencialesAsync(TestRfc);

        // Assert
        Assert.Equal(TestRfc, credenciales.Rfc);
        Assert.Equal(cerBytes, credenciales.CertificadoDer);
        Assert.Equal(keyBytes, credenciales.LlavePrivadaDer);
        Assert.Equal(password, credenciales.PasswordLlave);
    }

    [Fact]
    public async Task ObtenerCredencialesAsync_CertificadoFaltante_LanzaEmisorCredencialesException()
    {
        // Arrange - only create llave, no certificado
        CreateLlaveFile();
        Environment.SetEnvironmentVariable(EnvVarName, "password");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EmisorCredencialesException>(
            () => _sut.ObtenerCredencialesAsync(TestRfc));

        Assert.Contains("certificado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerCredencialesAsync_LlaveFaltante_LanzaEmisorCredencialesException()
    {
        // Arrange - only create certificado, no llave
        CreateCertificadoFile();
        Environment.SetEnvironmentVariable(EnvVarName, "password");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EmisorCredencialesException>(
            () => _sut.ObtenerCredencialesAsync(TestRfc));

        Assert.Contains("llave privada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerCredencialesAsync_VariableEntornoFaltante_LanzaEmisorCredencialesException()
    {
        // Arrange - create both files but no env var
        CreateCertificadoFile();
        CreateLlaveFile();
        Environment.SetEnvironmentVariable(EnvVarName, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EmisorCredencialesException>(
            () => _sut.ObtenerCredencialesAsync(TestRfc));

        Assert.Contains(EnvVarName, ex.Message);
    }

    [Fact]
    public void ExistenCredenciales_ConArchivos_RetornaTrue()
    {
        // Arrange
        CreateCertificadoFile();
        CreateLlaveFile();

        // Act
        var result = _sut.ExistenCredenciales(TestRfc);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExistenCredenciales_SinArchivos_RetornaFalse()
    {
        // Arrange - empty temp dir, no RFC subfolder

        // Act
        var result = _sut.ExistenCredenciales(TestRfc);

        // Assert
        Assert.False(result);
    }
}

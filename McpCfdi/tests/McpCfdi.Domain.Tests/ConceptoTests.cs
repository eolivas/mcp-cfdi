using McpCfdi.Domain.Entities;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class ConceptoTests
{
    private static ClaveCatalogo ValidClaveProdServ() => new("c_ClaveProdServ", "01010101");
    private static ClaveCatalogo ValidClaveUnidad() => new("c_ClaveUnidad", "H87");
    private static ClaveCatalogo ValidObjetoImp() => new("c_ObjetoImp", "02");
    private static MontoMoneda ValidMontoMoneda(decimal valor = 100m) => new(valor, 2);

    private static Concepto CreateValidConcepto(
        ClaveCatalogo? claveProdServ = null,
        decimal cantidad = 1m,
        ClaveCatalogo? claveUnidad = null,
        string descripcion = "Producto de prueba",
        MontoMoneda? valorUnitario = null,
        MontoMoneda? importe = null,
        ClaveCatalogo? objetoImp = null,
        string? noIdentificacion = null,
        string? unidad = null,
        MontoMoneda? descuento = null)
    {
        return new Concepto(
            claveProdServ ?? ValidClaveProdServ(),
            cantidad,
            claveUnidad ?? ValidClaveUnidad(),
            descripcion,
            valorUnitario ?? ValidMontoMoneda(),
            importe ?? ValidMontoMoneda(),
            objetoImp ?? ValidObjetoImp(),
            noIdentificacion,
            unidad,
            descuento);
    }

    [Fact]
    public void Constructor_WithValidArgs_CreatesConcepto()
    {
        var concepto = CreateValidConcepto();

        Assert.NotEqual(Guid.Empty, concepto.Id);
        Assert.Equal("01010101", concepto.ClaveProdServ.Clave);
        Assert.Equal(1m, concepto.Cantidad);
        Assert.Equal("H87", concepto.ClaveUnidad.Clave);
        Assert.Equal("Producto de prueba", concepto.Descripcion);
        Assert.Equal(100m, concepto.ValorUnitario.Valor);
        Assert.Equal(100m, concepto.Importe.Valor);
        Assert.Equal("02", concepto.ObjetoImp.Clave);
        Assert.Null(concepto.NoIdentificacion);
        Assert.Null(concepto.Unidad);
        Assert.Null(concepto.Descuento);
        Assert.Empty(concepto.Traslados);
    }

    [Fact]
    public void Constructor_WithOptionalArgs_SetsOptionalProperties()
    {
        var descuento = new MontoMoneda(10m, 2);

        var concepto = CreateValidConcepto(
            noIdentificacion: "SKU-001",
            unidad: "Pieza",
            descuento: descuento);

        Assert.Equal("SKU-001", concepto.NoIdentificacion);
        Assert.Equal("Pieza", concepto.Unidad);
        Assert.Equal(10m, concepto.Descuento!.Valor);
    }

    [Fact]
    public void Constructor_TrimsDescripcion()
    {
        var concepto = CreateValidConcepto(descripcion: "  Producto con espacios  ");

        Assert.Equal("Producto con espacios", concepto.Descripcion);
    }

    [Fact]
    public void Constructor_WithNullClaveProdServ_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Concepto(
            null!, 1m, ValidClaveUnidad(), "Desc", ValidMontoMoneda(), ValidMontoMoneda(), ValidObjetoImp()));
        Assert.Equal("claveProdServ", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullClaveUnidad_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Concepto(
            ValidClaveProdServ(), 1m, null!, "Desc", ValidMontoMoneda(), ValidMontoMoneda(), ValidObjetoImp()));
        Assert.Equal("claveUnidad", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullValorUnitario_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Concepto(
            ValidClaveProdServ(), 1m, ValidClaveUnidad(), "Desc", null!, ValidMontoMoneda(), ValidObjetoImp()));
        Assert.Equal("valorUnitario", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullImporte_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Concepto(
            ValidClaveProdServ(), 1m, ValidClaveUnidad(), "Desc", ValidMontoMoneda(), null!, ValidObjetoImp()));
        Assert.Equal("importe", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullObjetoImp_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Concepto(
            ValidClaveProdServ(), 1m, ValidClaveUnidad(), "Desc", ValidMontoMoneda(), ValidMontoMoneda(), null!));
        Assert.Equal("objetoImp", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Constructor_WithCantidadNotGreaterThanZero_ThrowsArgumentException(decimal cantidad)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValidConcepto(cantidad: cantidad));
        Assert.Equal("cantidad", ex.ParamName);
    }

    [Theory]
    [InlineData(1.1234567)]  // 7 decimal places
    [InlineData(0.12345678)] // 8 decimal places
    public void Constructor_WithCantidadMoreThan6Decimals_ThrowsArgumentException(decimal cantidad)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValidConcepto(cantidad: cantidad));
        Assert.Equal("cantidad", ex.ParamName);
    }

    [Theory]
    [InlineData(1.123456)]  // exactly 6 decimals
    [InlineData(1.12)]      // 2 decimals
    [InlineData(5)]         // no decimals
    public void Constructor_WithCantidadUpTo6Decimals_Succeeds(decimal cantidad)
    {
        var concepto = CreateValidConcepto(cantidad: cantidad);

        Assert.Equal(cantidad, concepto.Cantidad);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WithNullOrWhitespaceDescripcion_ThrowsArgumentException(string? descripcion)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValidConcepto(descripcion: descripcion!));
        Assert.Equal("descripcion", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithDescripcionExceeding1000Chars_ThrowsArgumentException()
    {
        var descripcion = new string('A', 1001);

        var ex = Assert.Throws<ArgumentException>(() => CreateValidConcepto(descripcion: descripcion));
        Assert.Equal("descripcion", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithDescripcionExactly1000Chars_Succeeds()
    {
        var descripcion = new string('A', 1000);

        var concepto = CreateValidConcepto(descripcion: descripcion);

        Assert.Equal(1000, concepto.Descripcion.Length);
    }

    [Fact]
    public void Constructor_WithSingleCharDescripcion_Succeeds()
    {
        var concepto = CreateValidConcepto(descripcion: "X");

        Assert.Equal("X", concepto.Descripcion);
    }

    [Fact]
    public void Constructor_WithNoIdentificacionExceeding100Chars_ThrowsArgumentException()
    {
        var noId = new string('A', 101);

        var ex = Assert.Throws<ArgumentException>(() => CreateValidConcepto(noIdentificacion: noId));
        Assert.Equal("noIdentificacion", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNoIdentificacionExactly100Chars_Succeeds()
    {
        var noId = new string('A', 100);

        var concepto = CreateValidConcepto(noIdentificacion: noId);

        Assert.Equal(100, concepto.NoIdentificacion!.Length);
    }

    [Fact]
    public void Constructor_WithUnidadExceeding20Chars_ThrowsArgumentException()
    {
        var unidad = new string('A', 21);

        var ex = Assert.Throws<ArgumentException>(() => CreateValidConcepto(unidad: unidad));
        Assert.Equal("unidad", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithUnidadExactly20Chars_Succeeds()
    {
        var unidad = new string('A', 20);

        var concepto = CreateValidConcepto(unidad: unidad);

        Assert.Equal(20, concepto.Unidad!.Length);
    }

    [Fact]
    public void AgregarTraslado_WithValidTraslado_AddsToCollection()
    {
        var concepto = CreateValidConcepto();
        var traslado = new TrasladoConcepto(
            new MontoMoneda(100m, 2),
            new ClaveCatalogo("c_Impuesto", "002"),
            new ClaveCatalogo("c_TipoFactor", "Tasa"),
            0.16m,
            new MontoMoneda(16m, 2));

        concepto.AgregarTraslado(traslado);

        Assert.Single(concepto.Traslados);
        Assert.Same(traslado, concepto.Traslados[0]);
    }

    [Fact]
    public void AgregarTraslado_WithNullTraslado_ThrowsArgumentNullException()
    {
        var concepto = CreateValidConcepto();

        Assert.Throws<ArgumentNullException>(() => concepto.AgregarTraslado(null!));
    }

    [Fact]
    public void AgregarTraslado_MultipleTraslados_AddsAll()
    {
        var concepto = CreateValidConcepto();
        var traslado1 = new TrasladoConcepto(
            new MontoMoneda(100m, 2),
            new ClaveCatalogo("c_Impuesto", "002"),
            new ClaveCatalogo("c_TipoFactor", "Tasa"),
            0.16m,
            new MontoMoneda(16m, 2));
        var traslado2 = new TrasladoConcepto(
            new MontoMoneda(100m, 2),
            new ClaveCatalogo("c_Impuesto", "003"),
            new ClaveCatalogo("c_TipoFactor", "Tasa"),
            0.08m,
            new MontoMoneda(8m, 2));

        concepto.AgregarTraslado(traslado1);
        concepto.AgregarTraslado(traslado2);

        Assert.Equal(2, concepto.Traslados.Count);
    }
}

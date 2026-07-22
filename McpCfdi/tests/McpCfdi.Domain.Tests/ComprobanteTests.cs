using McpCfdi.Domain.Entities;
using McpCfdi.Domain.ValueObjects;
using Xunit;

namespace McpCfdi.Domain.Tests;

public class ComprobanteTests
{
    private static readonly DateTime FechaValida = new(2024, 1, 15, 10, 30, 0);

    private static ClaveCatalogo FormaPago() => new("c_FormaPago", "01");
    private static ClaveCatalogo Moneda() => new("c_Moneda", "MXN");
    private static ClaveCatalogo TipoDeComprobante() => new("c_TipoDeComprobante", "I");
    private static ClaveCatalogo MetodoPago() => new("c_MetodoPago", "PUE");
    private static CodigoPostal LugarExpedicion() => new("06600");
    private static ClaveCatalogo Exportacion() => new("c_Exportacion", "01");

    private static Emisor CrearEmisor() => new(
        new Rfc("EKU9003173C9"),
        "Empresa de Prueba SA de CV",
        new ClaveCatalogo("c_RegimenFiscal", "601"));

    private static Receptor CrearReceptor() => new(
        new Rfc("XAXX010101000"),
        "Público General",
        new CodigoPostal("06600"),
        new ClaveCatalogo("c_RegimenFiscal", "616"),
        new ClaveCatalogo("c_UsoCFDI", "S01"));

    private static Concepto CrearConcepto(
        decimal importe = 100m,
        MontoMoneda? descuento = null,
        string objetoImpClave = "02",
        bool agregarTraslado = true)
    {
        var concepto = new Concepto(
            new ClaveCatalogo("c_ClaveProdServ", "01010101"),
            1m,
            new ClaveCatalogo("c_ClaveUnidad", "H87"),
            "Producto de prueba",
            new MontoMoneda(importe, 2),
            new MontoMoneda(importe, 2),
            new ClaveCatalogo("c_ObjetoImp", objetoImpClave),
            descuento: descuento);

        if (agregarTraslado && objetoImpClave == "02")
        {
            concepto.AgregarTraslado(new TrasladoConcepto(
                new MontoMoneda(importe, 2),
                new ClaveCatalogo("c_Impuesto", "002"),
                new ClaveCatalogo("c_TipoFactor", "Tasa"),
                0.16m,
                new MontoMoneda(importe * 0.16m, 2)));
        }

        return concepto;
    }

    private static List<Concepto> CrearConceptosValidos() => [CrearConcepto()];

    private static Comprobante CrearComprobanteValido(List<Concepto>? conceptos = null)
    {
        return Comprobante.Crear(
            FechaValida,
            FormaPago(),
            Moneda(),
            TipoDeComprobante(),
            MetodoPago(),
            LugarExpedicion(),
            Exportacion(),
            CrearEmisor(),
            CrearReceptor(),
            conceptos ?? CrearConceptosValidos());
    }

    #region Crear - Valid parameters

    [Fact]
    public void Crear_ConParametrosValidos_CreaInstancia()
    {
        var comprobante = CrearComprobanteValido();

        Assert.NotEqual(Guid.Empty, comprobante.Id);
        Assert.Equal(FechaValida, comprobante.Fecha);
        Assert.Equal("01", comprobante.FormaPago.Clave);
        Assert.Equal("MXN", comprobante.Moneda.Clave);
        Assert.Equal("I", comprobante.TipoDeComprobante.Clave);
        Assert.Equal("PUE", comprobante.MetodoPago.Clave);
        Assert.Equal("06600", comprobante.LugarExpedicion.Valor);
        Assert.Equal("01", comprobante.Exportacion.Clave);
        Assert.NotNull(comprobante.Emisor);
        Assert.NotNull(comprobante.Receptor);
        Assert.Single(comprobante.Conceptos);
        Assert.Null(comprobante.Sello);
        Assert.Equal(string.Empty, comprobante.NoCertificado);
        Assert.Equal(string.Empty, comprobante.Certificado);
    }

    #endregion

    #region Crear - Empty conceptos

    [Fact]
    public void Crear_ConListaDeConceptosVacia_LanzaArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CrearComprobanteValido(conceptos: []));

        Assert.Equal("conceptos", ex.ParamName);
    }

    #endregion

    #region Crear - Null parameters

    [Fact]
    public void Crear_ConFormaPagoNull_LanzaArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Comprobante.Crear(FechaValida, null!, Moneda(), TipoDeComprobante(),
                MetodoPago(), LugarExpedicion(), Exportacion(),
                CrearEmisor(), CrearReceptor(), CrearConceptosValidos()));
        Assert.Equal("formaPago", ex.ParamName);
    }

    [Fact]
    public void Crear_ConMonedaNull_LanzaArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Comprobante.Crear(FechaValida, FormaPago(), null!, TipoDeComprobante(),
                MetodoPago(), LugarExpedicion(), Exportacion(),
                CrearEmisor(), CrearReceptor(), CrearConceptosValidos()));
        Assert.Equal("moneda", ex.ParamName);
    }

    [Fact]
    public void Crear_ConEmisorNull_LanzaArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Comprobante.Crear(FechaValida, FormaPago(), Moneda(), TipoDeComprobante(),
                MetodoPago(), LugarExpedicion(), Exportacion(),
                null!, CrearReceptor(), CrearConceptosValidos()));
        Assert.Equal("emisor", ex.ParamName);
    }

    [Fact]
    public void Crear_ConReceptorNull_LanzaArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Comprobante.Crear(FechaValida, FormaPago(), Moneda(), TipoDeComprobante(),
                MetodoPago(), LugarExpedicion(), Exportacion(),
                CrearEmisor(), null!, CrearConceptosValidos()));
        Assert.Equal("receptor", ex.ParamName);
    }

    [Fact]
    public void Crear_ConConceptosNull_LanzaArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Comprobante.Crear(FechaValida, FormaPago(), Moneda(), TipoDeComprobante(),
                MetodoPago(), LugarExpedicion(), Exportacion(),
                CrearEmisor(), CrearReceptor(), null!));
        Assert.Equal("conceptos", ex.ParamName);
    }

    #endregion

    #region CalcularTotales - SubTotal

    [Fact]
    public void CalcularTotales_ConUnConcepto_CalculaSubTotalCorrecto()
    {
        var comprobante = CrearComprobanteValido([CrearConcepto(importe: 250.50m)]);

        comprobante.CalcularTotales(2);

        Assert.Equal(250.50m, comprobante.SubTotal.Valor);
        Assert.Equal(2, comprobante.SubTotal.Decimales);
    }

    [Fact]
    public void CalcularTotales_ConMultiplesConceptos_SumaImportes()
    {
        var conceptos = new List<Concepto>
        {
            CrearConcepto(importe: 100m),
            CrearConcepto(importe: 200m),
            CrearConcepto(importe: 300m)
        };
        var comprobante = CrearComprobanteValido(conceptos);

        comprobante.CalcularTotales(2);

        Assert.Equal(600m, comprobante.SubTotal.Valor);
    }

    #endregion

    #region CalcularTotales - Descuento

    [Fact]
    public void CalcularTotales_ConConceptosConDescuento_CalculaDescuento()
    {
        var conceptos = new List<Concepto>
        {
            CrearConcepto(importe: 100m, descuento: new MontoMoneda(10m, 2)),
            CrearConcepto(importe: 200m, descuento: new MontoMoneda(20m, 2))
        };
        var comprobante = CrearComprobanteValido(conceptos);

        comprobante.CalcularTotales(2);

        Assert.NotNull(comprobante.Descuento);
        Assert.Equal(30m, comprobante.Descuento.Valor);
    }

    [Fact]
    public void CalcularTotales_SinConceptosConDescuento_DescuentoEsNull()
    {
        var comprobante = CrearComprobanteValido([CrearConcepto(importe: 100m)]);

        comprobante.CalcularTotales(2);

        Assert.Null(comprobante.Descuento);
    }

    #endregion

    #region CalcularTotales - Impuestos globales

    [Fact]
    public void CalcularTotales_ConConceptosObjetoImp02_CalculaImpuestosGlobales()
    {
        var conceptos = new List<Concepto>
        {
            CrearConcepto(importe: 100m, objetoImpClave: "02"),
            CrearConcepto(importe: 200m, objetoImpClave: "02")
        };
        var comprobante = CrearComprobanteValido(conceptos);

        comprobante.CalcularTotales(2);

        Assert.NotNull(comprobante.Impuestos);
        // 100*0.16 + 200*0.16 = 16 + 32 = 48
        Assert.Equal(48m, comprobante.Impuestos.TotalImpuestosTrasladados.Valor);
        Assert.Single(comprobante.Impuestos.Traslados);
        // Base = 100 + 200 = 300
        Assert.Equal(300m, comprobante.Impuestos.Traslados[0].Base.Valor);
        Assert.Equal(48m, comprobante.Impuestos.Traslados[0].Importe!.Valor);
    }

    [Fact]
    public void CalcularTotales_ConDiferentesGruposDeImpuestos_AgrupaCorrectamente()
    {
        // Concepto 1: IVA 16%
        var concepto1 = new Concepto(
            new ClaveCatalogo("c_ClaveProdServ", "01010101"),
            1m,
            new ClaveCatalogo("c_ClaveUnidad", "H87"),
            "Producto 1",
            new MontoMoneda(100m, 2),
            new MontoMoneda(100m, 2),
            new ClaveCatalogo("c_ObjetoImp", "02"));
        concepto1.AgregarTraslado(new TrasladoConcepto(
            new MontoMoneda(100m, 2),
            new ClaveCatalogo("c_Impuesto", "002"),
            new ClaveCatalogo("c_TipoFactor", "Tasa"),
            0.16m,
            new MontoMoneda(16m, 2)));

        // Concepto 2: IEPS 8%
        var concepto2 = new Concepto(
            new ClaveCatalogo("c_ClaveProdServ", "01010101"),
            1m,
            new ClaveCatalogo("c_ClaveUnidad", "H87"),
            "Producto 2",
            new MontoMoneda(200m, 2),
            new MontoMoneda(200m, 2),
            new ClaveCatalogo("c_ObjetoImp", "02"));
        concepto2.AgregarTraslado(new TrasladoConcepto(
            new MontoMoneda(200m, 2),
            new ClaveCatalogo("c_Impuesto", "003"),
            new ClaveCatalogo("c_TipoFactor", "Tasa"),
            0.08m,
            new MontoMoneda(16m, 2)));

        var comprobante = CrearComprobanteValido([concepto1, concepto2]);

        comprobante.CalcularTotales(2);

        Assert.NotNull(comprobante.Impuestos);
        Assert.Equal(2, comprobante.Impuestos.Traslados.Count);
        // TotalImpuestosTrasladados = 16 + 16 = 32
        Assert.Equal(32m, comprobante.Impuestos.TotalImpuestosTrasladados.Valor);
    }

    [Fact]
    public void CalcularTotales_SinConceptosConObjetoImp02_ImpuestosEsNull()
    {
        var comprobante = CrearComprobanteValido([
            CrearConcepto(importe: 100m, objetoImpClave: "01", agregarTraslado: false)
        ]);

        comprobante.CalcularTotales(2);

        Assert.Null(comprobante.Impuestos);
    }

    #endregion

    #region CalcularTotales - Total

    [Fact]
    public void CalcularTotales_SinDescuentoNiImpuestos_TotalIgualSubTotal()
    {
        var comprobante = CrearComprobanteValido([
            CrearConcepto(importe: 500m, objetoImpClave: "01", agregarTraslado: false)
        ]);

        comprobante.CalcularTotales(2);

        Assert.Equal(500m, comprobante.Total.Valor);
    }

    [Fact]
    public void CalcularTotales_ConDescuentoYImpuestos_CalculaTotalCorrecto()
    {
        // Importe 1000, Descuento 100, IVA 16% sobre 1000 = 160
        // Total = 1000 - 100 + 160 = 1060
        var concepto = new Concepto(
            new ClaveCatalogo("c_ClaveProdServ", "01010101"),
            1m,
            new ClaveCatalogo("c_ClaveUnidad", "H87"),
            "Producto",
            new MontoMoneda(1000m, 2),
            new MontoMoneda(1000m, 2),
            new ClaveCatalogo("c_ObjetoImp", "02"),
            descuento: new MontoMoneda(100m, 2));
        concepto.AgregarTraslado(new TrasladoConcepto(
            new MontoMoneda(1000m, 2),
            new ClaveCatalogo("c_Impuesto", "002"),
            new ClaveCatalogo("c_TipoFactor", "Tasa"),
            0.16m,
            new MontoMoneda(160m, 2)));

        var comprobante = CrearComprobanteValido([concepto]);

        comprobante.CalcularTotales(2);

        Assert.Equal(1000m, comprobante.SubTotal.Valor);
        Assert.Equal(100m, comprobante.Descuento!.Valor);
        Assert.Equal(160m, comprobante.Impuestos!.TotalImpuestosTrasladados.Valor);
        Assert.Equal(1060m, comprobante.Total.Valor);
    }

    #endregion

    #region AsignarSello

    [Fact]
    public void AsignarSello_ConParametrosValidos_AsignaPropiedades()
    {
        var comprobante = CrearComprobanteValido();
        var noCertificado = "12345678901234567890";

        comprobante.AsignarSello("sello-base64", "certificado-base64", noCertificado);

        Assert.Equal("sello-base64", comprobante.Sello);
        Assert.Equal("certificado-base64", comprobante.Certificado);
        Assert.Equal(noCertificado, comprobante.NoCertificado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AsignarSello_ConSelloInvalido_LanzaArgumentException(string? sello)
    {
        var comprobante = CrearComprobanteValido();

        var ex = Assert.Throws<ArgumentException>(() =>
            comprobante.AsignarSello(sello!, "cert", "12345678901234567890"));
        Assert.Equal("sello", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AsignarSello_ConCertificadoInvalido_LanzaArgumentException(string? certificado)
    {
        var comprobante = CrearComprobanteValido();

        var ex = Assert.Throws<ArgumentException>(() =>
            comprobante.AsignarSello("sello", certificado!, "12345678901234567890"));
        Assert.Equal("certificado", ex.ParamName);
    }

    [Theory]
    [InlineData("1234567890123456789")]   // 19 digits
    [InlineData("123456789012345678901")] // 21 digits
    [InlineData("1234567890ABCDEFGHIJ")]  // non-digits
    [InlineData("")]
    [InlineData("   ")]
    public void AsignarSello_ConNoCertificadoInvalido_LanzaArgumentException(string noCertificado)
    {
        var comprobante = CrearComprobanteValido();

        var ex = Assert.Throws<ArgumentException>(() =>
            comprobante.AsignarSello("sello", "cert", noCertificado));
        Assert.Equal("noCertificado", ex.ParamName);
    }

    #endregion
}

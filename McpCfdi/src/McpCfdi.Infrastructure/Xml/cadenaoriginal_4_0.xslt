<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:cfdi="http://www.sat.gob.mx/cfd/4">

  <xsl:output method="text" encoding="UTF-8"/>
  <xsl:strip-space elements="*"/>

  <!-- Main template: produces ||field1|field2|...|fieldN|| -->
  <xsl:template match="/">
    <xsl:text>||</xsl:text>
    <xsl:apply-templates select="/cfdi:Comprobante"/>
    <xsl:text>||</xsl:text>
  </xsl:template>

  <!-- Comprobante node attributes -->
  <xsl:template match="cfdi:Comprobante">
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Version"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Fecha"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@FormaPago"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@NoCertificado"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@SubTotal"/></xsl:call-template>
    <xsl:if test="@Descuento">
      <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Descuento"/></xsl:call-template>
    </xsl:if>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Moneda"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Total"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@TipoDeComprobante"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@MetodoPago"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@LugarExpedicion"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Exportacion"/></xsl:call-template>
    <!-- Traverse child nodes in XSD order -->
    <xsl:apply-templates select="cfdi:Emisor"/>
    <xsl:apply-templates select="cfdi:Receptor"/>
    <xsl:apply-templates select="cfdi:Conceptos/cfdi:Concepto"/>
    <xsl:apply-templates select="cfdi:Impuestos"/>
  </xsl:template>

  <!-- Emisor -->
  <xsl:template match="cfdi:Emisor">
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Rfc"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Nombre"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@RegimenFiscal"/></xsl:call-template>
  </xsl:template>

  <!-- Receptor -->
  <xsl:template match="cfdi:Receptor">
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Rfc"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Nombre"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@DomicilioFiscalReceptor"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@RegimenFiscalReceptor"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@UsoCFDI"/></xsl:call-template>
  </xsl:template>

  <!-- Concepto -->
  <xsl:template match="cfdi:Concepto">
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@ClaveProdServ"/></xsl:call-template>
    <xsl:if test="@NoIdentificacion">
      <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@NoIdentificacion"/></xsl:call-template>
    </xsl:if>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Cantidad"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@ClaveUnidad"/></xsl:call-template>
    <xsl:if test="@Unidad">
      <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Unidad"/></xsl:call-template>
    </xsl:if>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Descripcion"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@ValorUnitario"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Importe"/></xsl:call-template>
    <xsl:if test="@Descuento">
      <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Descuento"/></xsl:call-template>
    </xsl:if>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@ObjetoImp"/></xsl:call-template>
    <!-- Traslados at concept level -->
    <xsl:apply-templates select="cfdi:Impuestos/cfdi:Traslados/cfdi:Traslado"/>
  </xsl:template>

  <!-- Traslado (both concept-level and global) -->
  <xsl:template match="cfdi:Traslado">
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Base"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Impuesto"/></xsl:call-template>
    <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@TipoFactor"/></xsl:call-template>
    <xsl:if test="@TasaOCuota">
      <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@TasaOCuota"/></xsl:call-template>
    </xsl:if>
    <xsl:if test="@Importe">
      <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@Importe"/></xsl:call-template>
    </xsl:if>
  </xsl:template>

  <!-- Global Impuestos node -->
  <xsl:template match="cfdi:Comprobante/cfdi:Impuestos">
    <xsl:if test="@TotalImpuestosTrasladados">
      <xsl:call-template name="emit-field"><xsl:with-param name="value" select="@TotalImpuestosTrasladados"/></xsl:call-template>
    </xsl:if>
    <xsl:apply-templates select="cfdi:Traslados/cfdi:Traslado"/>
  </xsl:template>

  <!-- Named template to emit a single field value preceded by pipe separator -->
  <xsl:template name="emit-field">
    <xsl:param name="value"/>
    <xsl:if test="string-length($value) &gt; 0">
      <xsl:value-of select="translate(normalize-space($value), '&#xD;&#x9;', '')"/>
      <xsl:text>|</xsl:text>
    </xsl:if>
  </xsl:template>

</xsl:stylesheet>

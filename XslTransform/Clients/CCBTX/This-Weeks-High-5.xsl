<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="html" omit-xml-declaration="yes"/>
  <xsl:template match="channel">
    <xsl:for-each select="item">
      <div class="row">
        <div class="h1"><xsl:value-of select="title"/></div>
        <div class="h2"><xsl:value-of select="category"/></div>
        <div class="h3"><xsl:value-of select="pubdate"/></div>
        <div class="col-sm-12"><xsl:value-of select="description"/></div>
      </div>
    </xsl:for-each>
  </xsl:template>
</xsl:stylesheet>
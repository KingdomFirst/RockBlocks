<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="html" omit-xml-declaration="yes"/>
  <xsl:template match="bookstore">
    <div class="row"><div class="col-sm-6">Sorted Book Titles:</div></div>
    <xsl:apply-templates select="book">
      <xsl:sort select="title"/>
    </xsl:apply-templates>
  </xsl:template>
  <xsl:template match="book">
    <div class="row"><div class="col-sm-6">Title:  <xsl:value-of select="node()"/></div></div>
  </xsl:template>
</xsl:stylesheet>
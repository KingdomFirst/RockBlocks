<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:dc="http://purl.org/dc/elements/1.1/">
  <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes"/>
  <xsl:template match="*">
    <xsl:copy-of select="/" />
  </xsl:template>
</xsl:stylesheet>

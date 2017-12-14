<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="html" omit-xml-declaration="yes"/>
  <xsl:template match="tables">
    <table>
      <tbody>
        <hr>
          <th>MinistryId</th>
          <th>Ministry</th>
          <th>SeriesId</th>
          <th>Series</th>
          <th>SeriesImgSm</th>
          <th>SeriesImgLg</th>
        </hr>
        <xsl:for-each select="sertable">
          <tr>
            <td>
              <xsl:value-of select="ministry" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="ministry_text" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="id" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="series_name" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:choose>
                <xsl:when test="series_image_sm != '' and not(starts-with(series_image_sm, 'http'))">
                  http://s3.amazonaws.com/highlandsfellowship/<xsl:value-of select="series_image_sm" disable-output-escaping="yes"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="series_image_sm" disable-output-escaping="yes"/>
                </xsl:otherwise>
              </xsl:choose>
            </td>
            <td>
              <xsl:choose>
                <xsl:when test="series_image_lrg != '' and not(starts-with(series_image_lrg, 'http'))">
                  http://s3.amazonaws.com/highlandsfellowship/<xsl:value-of select="series_image_lrg" disable-output-escaping="yes"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="series_image_lrg" disable-output-escaping="yes"/>
                </xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>
        </xsl:for-each>
      </tbody>
    </table>
  </xsl:template>
</xsl:stylesheet>
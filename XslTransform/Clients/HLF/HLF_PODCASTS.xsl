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
          <th>StudyId</th>
          <th>Study</th>
          <th>SpeakerId</th>
          <th>Speaker</th>
          <th>Description</th>
          <th>Text</th>
          <th>Slug</th>
          <th>Date</th>
          <th>Tags</th>
          <th>VimeoId</th>
          <th>Audio</th>
          <th>Notes</th>
          <th>ImgSm</th>
          <th>ImgMd</th>
          <th>ImgLg</th>
        </hr>
        <xsl:for-each select="mestable">
          <tr>
            <td>
              <xsl:value-of select="ministry" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="ministry_text" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="series" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="series_text" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="id" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="study_name" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="teacher" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="teacher_text" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="study_description" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="study_text" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="study_alias" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="study_date" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="tags" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="video_link" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="audio_link" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:value-of select="notes_link" disable-output-escaping="yes"/>
            </td>
            <td>
              <xsl:choose>
                <xsl:when test="imagesm != '' and not(starts-with(imagesm, 'http'))">
                  http://s3.amazonaws.com/highlandsfellowship/<xsl:value-of select="imagesm" disable-output-escaping="yes"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="imagesm" disable-output-escaping="yes"/>
                </xsl:otherwise>
              </xsl:choose>
            </td>
            <td>
              <xsl:choose>
                <xsl:when test="imagemed != '' and not(starts-with(imagemed, 'http'))">
                  http://s3.amazonaws.com/highlandsfellowship/<xsl:value-of select="imagemed" disable-output-escaping="yes"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="imagemed" disable-output-escaping="yes"/>
                </xsl:otherwise>
              </xsl:choose>
            </td>
            <td>
              <xsl:choose>
                <xsl:when test="imagelrg != '' and not(starts-with(imagelrg, 'http'))">
                  http://s3.amazonaws.com/highlandsfellowship/<xsl:value-of select="imagelrg" disable-output-escaping="yes"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:value-of select="imagelrg" disable-output-escaping="yes"/>
                </xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>
        </xsl:for-each>
      </tbody>
    </table>
  </xsl:template>
</xsl:stylesheet>
<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:param name="input-document" as="document-node()" required="yes"/>
  <xsl:template match="/">
    <result>
      <xsl:value-of select="$input-document/test1/id"/>
    </result>
  </xsl:template>
</xsl:stylesheet>

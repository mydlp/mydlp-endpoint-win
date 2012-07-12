<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                exclude-result-prefixes="msxsl"                
                xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">
  <xsl:output method="xml" indent="no" />

  <!-- MyDLP msi setup build xsl filter for heat output -->
  
  <!-- Default template copy all elements-->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Suppress copy of Component nodes where File child node with Source attribute ends with file specified extension -->
  <xsl:template match="wix:Component[wix:File[substring(@Source, string-length(@Source) - 3) = '.erl']]"/>
  <xsl:template match="wix:Component[wix:File[substring(@Source, string-length(@Source) - 1) = '.c']]"/>
  <xsl:template match="wix:Component[wix:File[substring(@Source, string-length(@Source) - 1) = '.h']]"/>
  <xsl:template match="wix:Component[wix:File[substring(@Source, string-length(@Source) - 3) = '.cpp']]"/>
  <xsl:template match="wix:Component[wix:File[substring(@Source, string-length(@Source) - 3) = '.git']]"/>
  <xsl:template match="wix:Component[wix:File[substring(@Source, string-length(@Source) - 3) = '.svn']]"/>
  <xsl:template match="wix:Component[wix:File[substring(@Source, string-length(@Source) - 13) = '.gitattributes']]"/>
  <xsl:template match="wix:Component[wix:File[substring(@Source, string-length(@Source) - 0) = '~']]"/>
    
</xsl:stylesheet>
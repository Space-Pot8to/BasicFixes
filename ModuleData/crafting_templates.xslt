<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
    <xsl:output omit-xml-declaration="yes"/>
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="CraftingTemplate[@id='TwoHandedPolearm' or @id='Pike' or @id='Javelin']/UsablePieces">
        <xsl:copy>
            <!-- And everything inside it -->
            <xsl:apply-templates select="@* | *"/>
            
            <!-- Add new node -->
            <xsl:element name="AvailablePiece" >
                <xsl:attribute name="piece_id">real_pilum_head</xsl:attribute>
            </xsl:element>

            <xsl:element name="AvailablePiece" >
                <xsl:attribute name="piece_id">real_triangular_head</xsl:attribute>
            </xsl:element>
        </xsl:copy>
    </xsl:template>

</xsl:stylesheet>
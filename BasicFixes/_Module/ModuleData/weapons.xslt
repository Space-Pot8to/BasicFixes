<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
    <xsl:output omit-xml-declaration="yes"/>
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="CraftedItem[@id='imperial_throwing_spear_1_t4']/Pieces/Piece[@id='spear_blade_38']">
        <xsl:copy>
            <xsl:apply-templates select="@* | *"/>
            <xsl:attribute name="id">real_pilum_head</xsl:attribute>
            <xsl:attribute name="scale_factor">100</xsl:attribute>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="CraftedItem[@id='eastern_throwing_spear_2_t4']/Pieces/Piece[@id='spear_blade_13']">
        <xsl:copy>
            <xsl:apply-templates select="@* | *"/>
            <xsl:attribute name="id">real_triangular_head</xsl:attribute>
            <xsl:attribute name="scale_factor">90</xsl:attribute>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
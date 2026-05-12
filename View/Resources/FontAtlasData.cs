namespace EFP.View.Resources;

public sealed class FontAtlasData
{
    public string Texture { get; set; } = string.Empty;
    public float LineHeight { get; set; }
    public float BaseSize { get; set; }
    public List<FontGlyphData> Glyphs { get; set; } = [];
}

public sealed class FontGlyphData
{
    public int Codepoint { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public int Advance { get; set; }
}
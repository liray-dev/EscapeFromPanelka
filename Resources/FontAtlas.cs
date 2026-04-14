using System.Numerics;
using EFP.Rendering;
using EFP.Utilities;

namespace EFP.Resources;

public sealed class FontAtlas(Texture2D texture, float lineHeight, float baseSize, Dictionary<int, FontGlyph> glyphs)
{
    public Texture2D Texture { get; } = texture;
    public float LineHeight { get; } = lineHeight;
    public float BaseSize { get; } = baseSize;

    public FontGlyph GetGlyph(char character)
    {
        if (glyphs.TryGetValue(character, out var glyph) 
            || glyphs.TryGetValue('?', out glyph)) return glyph;
        
        return default;
    }

    public Vector2 MeasureText(string text, float scale)
    {
        if (string.IsNullOrEmpty(text)) return Vector2.Zero;

        var width = 0f;
        var maxWidth = 0f;
        var height = LineHeight * scale;

        foreach (var character in text)
        {
            if (character == '\n')
            {
                maxWidth = MathF.Max(maxWidth, width);
                width = 0f;
                height += LineHeight * scale;
                continue;
            }

            var glyph = GetGlyph(character);
            width += glyph.Advance * scale;
        }

        maxWidth = MathF.Max(maxWidth, width);
        return new Vector2(maxWidth, height);
    }
}

public readonly record struct FontGlyph(
    RectF PixelRect,
    RectF UvRect,
    float OffsetX,
    float OffsetY,
    float Advance);
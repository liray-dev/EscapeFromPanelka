namespace EFP.Utilities;

public readonly struct ColorRgba(float r, float g, float b, float a = 1.0f)
{
    public float R { get; } = r;
    public float G { get; } = g;
    public float B { get; } = b;
    public float A { get; } = a;

    public static ColorRgba White => new(1f, 1f, 1f);
    public static ColorRgba Black => new(0f, 0f, 0f);

    public static ColorRgba FromBytes(byte r, byte g, byte b, byte a = 255)
    {
        return new ColorRgba(r / 255f, g / 255f, b / 255f, a / 255f);
    }
}
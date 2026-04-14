using System.Numerics;

namespace EFP.Utilities;

public readonly struct RectF(float x, float y, float width, float height)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;

    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public Vector2 Center => new(X + Width * 0.5f, Y + Height * 0.5f);

    public bool Contains(Vector2 point)
    {
        return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
    }

    public RectF Inflate(float amount)
    {
        return new RectF(X - amount, Y - amount, Width + amount * 2f, Height + amount * 2f);
    }
}
namespace EFP.Utilities;

public static class MathHelperEx
{
    public static float DegreesToRadians(float degrees)
    {
        return degrees * MathF.PI / 180f;
    }

    public static float RadiansToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }

    public static float Clamp(float value, float min, float max)
    {
        return MathF.Max(min, MathF.Min(max, value));
    }
}
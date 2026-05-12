using System.Numerics;

namespace EFP.Model.Raid;

public readonly record struct CollisionBox2D(Vector2 Min, Vector2 Max)
{
    public bool IntersectsCircle(Vector2 center, float radius)
    {
        var closest = Vector2.Clamp(center, Min, Max);
        return Vector2.DistanceSquared(center, closest) < radius * radius;
    }

    public bool SegmentIntersects(Vector2 start, Vector2 end)
    {
        if (Contains(start) || Contains(end)) return true;

        var delta = end - start;
        var tMin = 0f;
        var tMax = 1f;

        return Clip(-delta.X, start.X - Min.X, ref tMin, ref tMax)
               && Clip(delta.X, Max.X - start.X, ref tMin, ref tMax)
               && Clip(-delta.Y, start.Y - Min.Y, ref tMin, ref tMax)
               && Clip(delta.Y, Max.Y - start.Y, ref tMin, ref tMax);
    }

    private bool Contains(Vector2 point)
    {
        return point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y;
    }

    private static bool Clip(float p, float q, ref float tMin, ref float tMax)
    {
        if (MathF.Abs(p) <= float.Epsilon) return q >= 0f;

        var t = q / p;
        if (p < 0f)
        {
            if (t > tMax) return false;
            if (t > tMin) tMin = t;
        }
        else
        {
            if (t < tMin) return false;
            if (t < tMax) tMax = t;
        }

        return true;
    }
}
using System.Numerics;

namespace EFP.World;

public readonly record struct CollisionBox2D(Vector2 Min, Vector2 Max)
{
    public bool IntersectsCircle(Vector2 center, float radius)
    {
        var closest = Vector2.Clamp(center, Min, Max);
        return Vector2.DistanceSquared(center, closest) < radius * radius;
    }
}

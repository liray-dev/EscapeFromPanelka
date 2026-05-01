using System.Numerics;

namespace EFP.World;

public sealed class InfectedZone(
    string id,
    string label,
    Vector3 center,
    float radius,
    RaidPressureLevel activationLevel,
    float moveMultiplier,
    float damagePerSecond,
    float visibilityBoost,
    WorldRenderable renderable)
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public Vector3 Center { get; } = center;
    public float Radius { get; } = radius;
    public RaidPressureLevel ActivationLevel { get; } = activationLevel;
    public float MoveMultiplier { get; } = moveMultiplier;
    public float DamagePerSecond { get; } = damagePerSecond;
    public float VisibilityBoost { get; } = visibilityBoost;
    public WorldRenderable Renderable { get; } = renderable;

    public bool IsActive(RaidPressureLevel level)
    {
        return (int)level >= (int)ActivationLevel;
    }

    public bool Contains(Vector3 worldPosition)
    {
        var offset = new Vector2(worldPosition.X - Center.X, worldPosition.Z - Center.Z);
        return offset.LengthSquared() <= Radius * Radius;
    }
}
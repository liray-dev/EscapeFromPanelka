using System.Numerics;

namespace EFP.World;

public sealed class InfectedZone
{
    public InfectedZone(string id, string label, Vector3 center, float radius, RaidPressureLevel activationLevel,
        float moveMultiplier, WorldRenderable renderable)
    {
        Id = id;
        Label = label;
        Center = center;
        Radius = radius;
        ActivationLevel = activationLevel;
        MoveMultiplier = moveMultiplier;
        Renderable = renderable;
    }

    public string Id { get; }
    public string Label { get; }
    public Vector3 Center { get; }
    public float Radius { get; }
    public RaidPressureLevel ActivationLevel { get; }
    public float MoveMultiplier { get; }
    public WorldRenderable Renderable { get; }

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
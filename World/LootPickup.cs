using System.Numerics;

namespace EFP.World;

public sealed class LootPickup(
    string id,
    string label,
    LootKind kind,
    Vector3 position,
    WorldRenderable renderable,
    int value,
    int medkitCount = 0)
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public LootKind Kind { get; } = kind;
    public Vector3 Position { get; } = position;
    public WorldRenderable Renderable { get; } = renderable;
    public int Value { get; } = value;
    public int MedkitCount { get; } = medkitCount;
    public bool Collected { get; private set; }

    public void Collect()
    {
        Collected = true;
    }
}
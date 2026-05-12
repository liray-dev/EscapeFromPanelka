using System.Numerics;
using EFP.Model.Common;

namespace EFP.Model.Raid;

public sealed class LootPickup : Entity
{
    public LootPickup(string id, string label, LootKind kind, Vector3 position, WorldRenderable renderable, int value,
        int medkitCount = 0, string? modelId = null)
        : base(id, label, position)
    {
        Kind = kind;
        Position = position;
        Renderable = renderable;
        Value = value;
        MedkitCount = medkitCount;
        ModelId = modelId;
    }

    public LootKind Kind { get; }
    public Vector3 Position { get; }
    public WorldRenderable Renderable { get; }
    public int Value { get; }
    public int MedkitCount { get; }
    public string? ModelId { get; }
    public bool Collected { get; private set; }

    public void Collect()
    {
        Collected = true;
        IsActive = false;
    }
}

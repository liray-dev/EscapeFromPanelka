using System.Numerics;
using EFP.Model.Raid.Objectives;

namespace EFP.Model.Raid;

public sealed class ExtractionPoint
{
    public ExtractionPoint(string id, string label, Vector3 position, RaidObjective condition, WorldRenderable marker)
    {
        Id = id;
        Label = label;
        Position = position;
        Condition = condition;
        Marker = marker;
    }

    public string Id { get; }
    public string Label { get; }
    public Vector3 Position { get; }
    public RaidObjective Condition { get; }
    public WorldRenderable Marker { get; }
    public bool Used { get; private set; }

    public void MarkUsed()
    {
        Used = true;
    }
}

namespace EFP.Model.WorldGen;

public sealed class SectorBuildState
{
    public StructureBlueprint Blueprint { get; init; } = new();
    public Dictionary<string, Placement> PlacedByNodeId { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<OpenSocket> OpenSockets { get; } = [];
    public int NextNodeIndex { get; set; }
}

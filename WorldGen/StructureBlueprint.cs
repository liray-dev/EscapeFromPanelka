namespace EFP.WorldGen;

public sealed class StructureBlueprint
{
    public int Seed { get; set; }
    public List<BlueprintStep> Steps { get; set; } = [];
}

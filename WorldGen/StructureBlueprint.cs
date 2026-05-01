namespace EFP.WorldGen;

public sealed class StructureBlueprint
{
    public int Seed { get; init; }
    public List<BlueprintStep> Steps { get; init; } = [];
}
namespace EFP.WorldGen;

public sealed class PropSocketDefinition
{
    public string Id { get; init; } = string.Empty;
    public string SlotType { get; init; } = string.Empty;
    public float LocalX { get; init; }
    public float LocalY { get; init; }
    public float LocalZ { get; init; }
    public float RotationDegrees { get; init; }
    public float SpawnChance { get; init; } = 1f;
    public List<string> AllowedProps { get; init; } = [];
}
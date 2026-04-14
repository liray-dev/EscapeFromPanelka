namespace EFP.WorldGen;

public sealed class PropSocketDefinition
{
    public string Id { get; set; } = string.Empty;
    public string SlotType { get; set; } = string.Empty;
    public float LocalX { get; set; }
    public float LocalY { get; set; }
    public float LocalZ { get; set; }
    public float RotationDegrees { get; set; }
    public float SpawnChance { get; set; } = 1f;
    public List<string> AllowedProps { get; set; } = [];
}

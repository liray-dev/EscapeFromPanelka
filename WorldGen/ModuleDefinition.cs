namespace EFP.WorldGen;

public sealed class ModuleDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public float Width { get; set; } = 4f;
    public float Length { get; set; } = 4f;
    public float FloorHeight { get; set; } = 0.04f;
    public float WallHeight { get; set; } = 3f;
    public float WallThickness { get; set; } = 0.18f;
    public float[] FloorColor { get; set; } = [0.2f, 0.2f, 0.2f, 1f];
    public float[] WallColor { get; set; } = [0.5f, 0.5f, 0.5f, 1f];
    public List<string> Tags { get; set; } = [];
    public List<ConnectionSocketDefinition> Connections { get; set; } = [];
    public List<PropSocketDefinition> PropSockets { get; set; } = [];
}

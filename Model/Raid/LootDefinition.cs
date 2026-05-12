namespace EFP.Model.Raid;

public sealed class LootDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public LootKind Kind { get; set; } = LootKind.Scrap;
    public int Value { get; set; }
    public int MedkitCount { get; set; }
    public float[] Size { get; set; } = [0.26f, 0.26f, 0.26f];
    public float[] Color { get; set; } = [0.7f, 0.7f, 0.7f, 1f];
    public string? ModelId { get; set; }
    public List<string> AllowedArchetypes { get; set; } = [];
    public float Weight { get; set; } = 1f;
}
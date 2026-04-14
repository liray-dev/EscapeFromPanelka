namespace EFP.WorldGen;

public sealed class PropDefinition
{
    public string Id { get; set; } = string.Empty;
    public string SlotType { get; set; } = string.Empty;
    public float[] Size { get; set; } = [1f, 1f, 1f];
    public float[] Color { get; set; } = [1f, 1f, 1f, 1f];
}
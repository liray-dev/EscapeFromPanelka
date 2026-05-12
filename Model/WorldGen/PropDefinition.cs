namespace EFP.Model.WorldGen;

public sealed class PropDefinition
{
    public string Id { get; init; } = string.Empty;
    public string SlotType { get; init; } = string.Empty;
    public float[] Size { get; init; } = [1f, 1f, 1f];
    public float[] Color { get; init; } = [1f, 1f, 1f, 1f];
}
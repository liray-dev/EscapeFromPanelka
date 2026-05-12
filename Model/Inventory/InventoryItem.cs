namespace EFP.Model.Inventory;

public sealed record InventoryItem
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public int Width { get; init; } = 1;
    public int Height { get; init; } = 1;
    public int Value { get; init; }
    public float[] Color { get; init; } = [0.6f, 0.6f, 0.6f, 1.0f];
}

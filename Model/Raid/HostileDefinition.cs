namespace EFP.Model.Raid;

public sealed class HostileDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public float MaxHealth { get; set; } = 60f;
    public float CollisionRadius { get; set; } = 0.34f;
    public float[] DormantTint { get; set; } = [0.34f, 0.37f, 0.42f, 1f];
    public float[] AlertTint { get; set; } = [0.78f, 0.28f, 0.32f, 1f];
    public float[] Size { get; set; } = [0.78f, 1.16f, 0.78f];
    public string? ModelId { get; set; }
    public List<string> AllowedArchetypes { get; set; } = [];
    public float Weight { get; set; } = 1f;
}
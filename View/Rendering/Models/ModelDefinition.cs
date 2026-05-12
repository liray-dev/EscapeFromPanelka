namespace EFP.View.Rendering.Models;

public sealed class ModelDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public float[] Scale { get; set; } = [1f, 1f, 1f];
    public float[] OffsetMeters { get; set; } = [0f, 0f, 0f];
    public float YawOffsetDegrees { get; set; }
    public float[] Tint { get; set; } = [1f, 1f, 1f, 1f];
    public bool FlipUv { get; set; }
}
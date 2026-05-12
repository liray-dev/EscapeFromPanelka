using System.Numerics;

namespace EFP.Model.Raid;

public sealed class WorldLight
{
    public WorldLight(string id, Vector3 position, Vector3 color, float radius, float intensity, float flickerSpeed,
        float phaseOffset, bool emergency)
    {
        Id = id;
        Position = position;
        Color = color;
        Radius = radius;
        Intensity = intensity;
        FlickerSpeed = flickerSpeed;
        PhaseOffset = phaseOffset;
        Emergency = emergency;
    }

    public string Id { get; }
    public Vector3 Position { get; }
    public Vector3 Color { get; }
    public float Radius { get; }
    public float Intensity { get; }
    public float FlickerSpeed { get; }
    public float PhaseOffset { get; }
    public bool Emergency { get; }
}
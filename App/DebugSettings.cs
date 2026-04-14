using System.Numerics;

namespace EFP.App;

public sealed class DebugSettings
{
    public bool Enabled { get; set; }
    public bool CaptureKeyboard { get; set; }
    public bool CaptureMouse { get; set; }
    public bool IgnoreCollisions { get; set; }
    public bool ShowDemoWindow { get; set; }
    public bool AllowCriticalMutation { get; set; } = true;
    public Vector3 LightColor { get; set; } = new(0.92f, 0.95f, 1.00f);
    public Vector3 FogColor { get; set; } = new(0.07f, 0.08f, 0.10f);
    public float AmbientStrength { get; set; } = 0.24f;
    public float DiffuseStrength { get; set; } = 0.54f;
    public float FogNear { get; set; } = 16.0f;
    public float FogFar { get; set; } = 52.0f;
}
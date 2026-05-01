namespace EFP.App;

public sealed class GameConfig
{
    public WindowConfig Window { get; set; } = new();
    public GraphicsConfig Graphics { get; set; } = new();
    public GameplayConfig Gameplay { get; set; } = new();
    public CameraConfig Camera { get; set; } = new();
}

public sealed class WindowConfig
{
    public string Title { get; set; } = "Escape From Panelka";
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public bool VSync { get; set; }
}

public sealed class GraphicsConfig
{
    public float[] ClearColor { get; set; } = [0.05f, 0.06f, 0.08f, 1.0f];
}

public sealed class GameplayConfig
{
    public int FixedTickRate { get; set; } = 60;
    public float PlayerMoveSpeed { get; set; } = 5.0f;
    public float PlayerRotationSensitivity { get; set; } = 0.01f;
    public float FloorSize { get; set; } = 24.0f;
    public float PlayerCollisionRadius { get; set; } = 0.34f;
    public float InteractRadius { get; set; } = 1.6f;
    public float RaidDurationSeconds { get; set; } = 165.0f;
    public float PressureThresholdSeconds { get; set; } = 90.0f;
    public float CriticalThresholdSeconds { get; set; } = 42.0f;
    public float PlayerMaxHealth { get; set; } = 100.0f;
    public int StartingMedkits { get; set; } = 0;
    public float MedkitHealAmount { get; set; } = 35.0f;
    public float QuietWalkMultiplier { get; set; } = 0.46f;
    public float NoiseDecayRate { get; set; } = 4.4f;
    public float HostileContactDamage { get; set; } = 18.0f;
    public float HostileContactCooldownSeconds { get; set; } = 0.70f;
    public float InfectedDamageMultiplier { get; set; } = 1.0f;
    public int ObjectiveLootValue { get; set; } = 120;
}

public sealed class CameraConfig
{
    public float Height { get; set; } = 13.5f;
    public float Distance { get; set; } = 10.0f;
    public float YawDegrees { get; set; } = -38.0f;
    public float PitchDegrees { get; set; } = 58.0f;
    public float FovDegrees { get; set; } = 48.0f;
}
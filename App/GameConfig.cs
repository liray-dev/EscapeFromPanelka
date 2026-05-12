namespace EFP.App;

public sealed class GameConfig
{
    public WindowConfig Window { get; set; } = new();
    public GraphicsConfig Graphics { get; set; } = new();
    public GameplayConfig Gameplay { get; set; } = new();
    public CameraConfig Camera { get; set; } = new();
    public DebugConfig Debug { get; set; } = new();
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

    public float AttackChargeRate { get; set; } = 1.10f;
    public float AttackCooldownSeconds { get; set; } = 0.50f;
    public float AttackBaseDamage { get; set; } = 22.0f;
    public float AttackCriticalMultiplier { get; set; } = 3.0f;
    public float AttackRange { get; set; } = 1.55f;
    public float AttackHalfFovDegrees { get; set; } = 55f;
    public float AttackCriticalWindowMin { get; set; } = 0.55f;
    public float AttackCriticalWindowMax { get; set; } = 0.85f;
    public float AttackCriticalWindowSize { get; set; } = 0.05f;
    public float AttackNoise { get; set; } = 0.62f;
    public float AttackCriticalNoise { get; set; } = 0.85f;

    public int MinModulesPerSector { get; set; } = 24;
    public int MaxModulesPerSector { get; set; } = 44;
}

public sealed class CameraConfig
{
    public float Height { get; set; } = 13.5f;
    public float Distance { get; set; } = 10.0f;
    public float YawDegrees { get; set; } = -38.0f;
    public float PitchDegrees { get; set; } = 58.0f;
    public float FovDegrees { get; set; } = 48.0f;
}

public sealed class DebugConfig
{
    public bool Enabled { get; set; }
    public bool IgnoreCollisions { get; set; }
    public bool ShowDemoWindow { get; set; }
    public bool AllowCriticalMutation { get; set; } = true;
    public float[] LightColor { get; set; } = [0.92f, 0.95f, 1.00f];
    public float[] FogColor { get; set; } = [0.07f, 0.08f, 0.10f];
    public float AmbientStrength { get; set; } = 0.24f;
    public float DiffuseStrength { get; set; } = 0.54f;
    public float FogNear { get; set; } = 16.0f;
    public float FogFar { get; set; } = 52.0f;
    public float CameraYawDegrees { get; set; } = -38.0f;
    public float CameraPitchDegrees { get; set; } = 58.0f;
    public float CameraDistance { get; set; } = 10.0f;
    public float CameraHeight { get; set; } = 13.5f;
    public float CameraFollowSmoothing { get; set; } = 1.0f;
    public float RoomSizeMultiplier { get; set; } = 1.0f;
    public float RotationSpeedMultiplier { get; set; } = 0.45f;

    public void ApplyTo(DebugSettings settings)
    {
        settings.Enabled = Enabled;
        settings.IgnoreCollisions = IgnoreCollisions;
        settings.ShowDemoWindow = ShowDemoWindow;
        settings.AllowCriticalMutation = AllowCriticalMutation;
        settings.LightColor = ToVector3(LightColor, 0.92f, 0.95f, 1.00f);
        settings.FogColor = ToVector3(FogColor, 0.07f, 0.08f, 0.10f);
        settings.AmbientStrength = AmbientStrength;
        settings.DiffuseStrength = DiffuseStrength;
        settings.FogNear = FogNear;
        settings.FogFar = MathF.Max(FogFar, FogNear + 1.0f);
        settings.CameraYawDegrees = CameraYawDegrees;
        settings.CameraPitchDegrees = CameraPitchDegrees;
        settings.CameraDistance = CameraDistance;
        settings.CameraHeight = CameraHeight;
        settings.CameraFollowSmoothing = CameraFollowSmoothing;
        settings.RoomSizeMultiplier = RoomSizeMultiplier;
        settings.RotationSpeedMultiplier = RotationSpeedMultiplier;
    }

    public void CaptureFrom(DebugSettings settings)
    {
        Enabled = settings.Enabled;
        IgnoreCollisions = settings.IgnoreCollisions;
        ShowDemoWindow = settings.ShowDemoWindow;
        AllowCriticalMutation = settings.AllowCriticalMutation;
        LightColor = [settings.LightColor.X, settings.LightColor.Y, settings.LightColor.Z];
        FogColor = [settings.FogColor.X, settings.FogColor.Y, settings.FogColor.Z];
        AmbientStrength = settings.AmbientStrength;
        DiffuseStrength = settings.DiffuseStrength;
        FogNear = settings.FogNear;
        FogFar = settings.FogFar;
        CameraYawDegrees = settings.CameraYawDegrees;
        CameraPitchDegrees = settings.CameraPitchDegrees;
        CameraDistance = settings.CameraDistance;
        CameraHeight = settings.CameraHeight;
        CameraFollowSmoothing = settings.CameraFollowSmoothing;
        RoomSizeMultiplier = settings.RoomSizeMultiplier;
        RotationSpeedMultiplier = settings.RotationSpeedMultiplier;
    }

    private static System.Numerics.Vector3 ToVector3(IReadOnlyList<float> values, float fallbackX, float fallbackY,
        float fallbackZ)
    {
        return new System.Numerics.Vector3(
            values.Count > 0 ? values[0] : fallbackX,
            values.Count > 1 ? values[1] : fallbackY,
            values.Count > 2 ? values[2] : fallbackZ);
    }
}

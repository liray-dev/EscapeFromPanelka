namespace EFP.Model.Raid;

public sealed record RaidSettings
{
    public float PlayerMoveSpeed { get; init; } = 5.0f;
    public float PlayerCollisionRadius { get; init; } = 0.34f;
    public float PlayerMaxHealth { get; init; } = 100.0f;
    public int StartingMedkits { get; init; }
    public float MedkitHealAmount { get; init; } = 35.0f;
    public float InteractRadius { get; init; } = 1.6f;
    public float RaidDurationSeconds { get; init; } = 165.0f;
    public float PressureThresholdSeconds { get; init; } = 90.0f;
    public float CriticalThresholdSeconds { get; init; } = 42.0f;
    public float QuietWalkMultiplier { get; init; } = 0.46f;
    public float NoiseDecayRate { get; init; } = 4.4f;
    public float HostileContactDamage { get; init; } = 18.0f;
    public float HostileContactCooldownSeconds { get; init; } = 0.70f;
    public float InfectedDamageMultiplier { get; init; } = 1.0f;
    public int ObjectiveLootValue { get; init; } = 120;

    public float AttackChargeRate { get; init; } = 1.10f;
    public float AttackCooldownSeconds { get; init; } = 0.50f;
    public float AttackBaseDamage { get; init; } = 22.0f;
    public float AttackCriticalMultiplier { get; init; } = 3.0f;
    public float AttackRange { get; init; } = 1.55f;
    public float AttackHalfFovDegrees { get; init; } = 55f;
    public float AttackCriticalWindowMin { get; init; } = 0.55f;
    public float AttackCriticalWindowMax { get; init; } = 0.85f;
    public float AttackCriticalWindowSize { get; init; } = 0.05f;
    public float AttackNoise { get; init; } = 0.62f;
    public float AttackCriticalNoise { get; init; } = 0.85f;

    public int MinModulesPerSector { get; init; } = 40;
    public int MaxModulesPerSector { get; init; } = 60;

    public static RaidSettings Default { get; } = new();
}

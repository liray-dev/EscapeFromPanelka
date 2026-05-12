namespace EFP.Model.Raid;

public sealed class PlayerCombat
{
    private readonly RaidSettings _config;
    private readonly Random _rng;
    private float _cooldownRemaining;
    private bool _previousLmbDown;

    public PlayerCombat(RaidSettings config, int seed)
    {
        _config = config;
        _rng = new Random(seed ^ 0x5A5A5A5A);
        ResetWindow();
    }

    public bool Charging { get; private set; }
    public float ChargeLevel { get; private set; }

    public float CooldownNormalized => _config.AttackCooldownSeconds <= 0.001f
        ? 0f
        : Math.Clamp(_cooldownRemaining / _config.AttackCooldownSeconds, 0f, 1f);

    public bool OnCooldown => _cooldownRemaining > 0f;
    public float WindowStart { get; private set; }

    public float WindowEnd => WindowStart + _config.AttackCriticalWindowSize;
    public AttackResult? LastAttack { get; private set; }

    public AttackResult? Tick(float deltaTime, bool lmbDown)
    {
        if (_cooldownRemaining > 0f) _cooldownRemaining = MathF.Max(0f, _cooldownRemaining - deltaTime);

        AttackResult? released = null;

        if (lmbDown && !_previousLmbDown && !OnCooldown)
        {
            Charging = true;
            ChargeLevel = 0f;
        }

        if (Charging)
        {
            if (lmbDown)
            {
                ChargeLevel = MathF.Min(1f, ChargeLevel + _config.AttackChargeRate * deltaTime);
            }
            else
            {
                released = ResolveRelease();
                Charging = false;
                ChargeLevel = 0f;
                _cooldownRemaining = _config.AttackCooldownSeconds;
                ResetWindow();
            }
        }
        else
        {
            ChargeLevel = 0f;
        }

        _previousLmbDown = lmbDown;
        if (released is not null) LastAttack = released;
        return released;
    }

    public void Reset()
    {
        Charging = false;
        ChargeLevel = 0f;
        _cooldownRemaining = 0f;
        _previousLmbDown = false;
        LastAttack = null;
        ResetWindow();
    }

    private AttackResult ResolveRelease()
    {
        var charge = ChargeLevel;
        var critical = charge >= WindowStart && charge <= WindowStart + _config.AttackCriticalWindowSize;
        var damage = _config.AttackBaseDamage * Math.Clamp(charge, 0.25f, 1f);
        if (critical) damage *= _config.AttackCriticalMultiplier;
        return new AttackResult(charge, critical, damage);
    }

    private void ResetWindow()
    {
        var min = MathF.Min(_config.AttackCriticalWindowMin, _config.AttackCriticalWindowMax);
        var max = MathF.Max(_config.AttackCriticalWindowMin, _config.AttackCriticalWindowMax);
        WindowStart = min + (max - min) * (float)_rng.NextDouble();
    }
}

public readonly record struct AttackResult(float Charge, bool Critical, float Damage);
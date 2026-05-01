using System.Numerics;
using EFP.App;
using EFP.Entities;
using EFP.WorldGen;

namespace EFP.World;

public sealed class World
{
    private const float CollisionCeilingHeight = 1.85f;
    private readonly GameplayConfig _config;
    private readonly List<CollisionBox2D> _criticalMutationColliders = [];
    private readonly WorldRenderable _extractionMarker;
    private readonly WorldRenderable _objectiveMarker;
    private readonly Dictionary<string, CollisionBox2D> _passageColliders = new(StringComparer.OrdinalIgnoreCase);
    private readonly WorldRenderable _powerSwitchMarker;
    private readonly List<CollisionBox2D> _solidColliders = [];
    private float _contactDamageCooldownSeconds;
    private string? _interactionLootId;
    private string? _interactionPassageId;
    private InteractionTarget _interactionTarget;
    private float _playerVisibilityBoost;
    private float _timeRemainingSeconds;

    public World(GameplayConfig config, ProceduralSector sector)
    {
        _config = config;
        Sector = sector;
        Player = new PlayerCube(config.PlayerMoveSpeed, config.PlayerCollisionRadius, sector.PlayerSpawn);
        PowerSwitchPoint = sector.PowerSwitchPoint;
        ObjectivePoint = sector.ObjectivePoint;
        ExtractionPoint = sector.ExtractionConsolePoint;
        _timeRemainingSeconds = config.RaidDurationSeconds;
        PlayerHealth = config.PlayerMaxHealth;
        MedkitCount = Math.Max(0, config.StartingMedkits);

        var halfExtents = Vector3.Max(Vector3.Abs(sector.BoundsMin), Vector3.Abs(sector.BoundsMax));
        var foundationSize = MathF.Max(halfExtents.X, halfExtents.Z) * 2f + 6f;
        Foundation = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = new Vector3(0f, -0.08f, 0f),
                Scale = new Vector3(foundationSize, 0.16f, foundationSize)
            },
            new Vector4(0.11f, 0.12f, 0.13f, 1f));

        _powerSwitchMarker = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = PowerSwitchPoint + new Vector3(0f, 0.55f, 0f),
                Scale = new Vector3(0.42f, 1.1f, 0.42f)
            },
            new Vector4(0.84f, 0.70f, 0.24f, 1f));

        _objectiveMarker = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = ObjectivePoint + new Vector3(0f, 0.55f, 0f),
                Scale = new Vector3(0.55f, 1.1f, 0.55f)
            },
            new Vector4(0.16f, 0.72f, 0.64f, 1f));

        _extractionMarker = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = ExtractionPoint + new Vector3(0f, 0.55f, 0f),
                Scale = new Vector3(0.55f, 1.1f, 0.55f)
            },
            new Vector4(0.84f, 0.71f, 0.22f, 1f));

        BuildSolidColliders();
        UpdatePlayerEnvironment(0f, Vector2.Zero, false);
        UpdateInteractionState();
    }

    public ProceduralSector Sector { get; }
    public PlayerCube Player { get; }
    public WorldRenderable Foundation { get; }
    public IReadOnlyList<WorldRenderable> StaticGeometry => Sector.StaticGeometry;
    public IReadOnlyList<WorldRenderable> FeatureGeometry => Sector.FeatureGeometry;
    public IReadOnlyList<WorldLight> Lights => Sector.Lights;
    public IReadOnlyList<HostileEntity> Hostiles => Sector.Hostiles;
    public IEnumerable<LootPickup> ActiveLootPickups => Sector.Loot.Where(x => !x.Collected);

    public IReadOnlyList<WorldRenderable> ActiveCriticalMutationGeometry =>
        CriticalMutationActive ? Sector.CriticalMutationGeometry : [];

    public IEnumerable<InfectedZone> ActiveInfectedZones => Sector.InfectedZones.Where(IsInfectedZoneActive);
    public IReadOnlyList<PropInstance> Props => Sector.Props;
    public IEnumerable<LockablePassage> ActiveLockablePassages => Sector.LockablePassages.Where(x => x.Visible);
    public int Seed => Sector.Seed;
    public int ModuleCount => Sector.Modules.Count;
    public int PropCount => Sector.Props.Count;
    public int DoorCount => Sector.LockablePassages.Count;
    public int LockedPassageCount => Sector.LockablePassages.Count(x => x.BlocksPassage);
    public int LightCount => Sector.Lights.Count;
    public int ActiveInfectedZoneCount => Sector.InfectedZones.Count(IsInfectedZoneActive);
    public int AlertedHostileCount => Sector.Hostiles.Count(x => x.Alerted);
    public int HuntingHostileCount => Sector.Hostiles.Count(x => x.AwarenessState == HostileAwarenessState.Hunt);
    public int SearchingHostileCount => Sector.Hostiles.Count(x => x.AwarenessState == HostileAwarenessState.Search);
    public int CargoCount => Sector.Loot.Count(x => x.Collected && x.Value > 0) + (ObjectiveRecovered ? 1 : 0);

    public int CargoValue => Sector.Loot.Where(x => x.Collected).Sum(x => x.Value) +
                             (ObjectiveRecovered ? _config.ObjectiveLootValue : 0);

    public Vector3 PowerSwitchPoint { get; }
    public Vector3 ObjectivePoint { get; }
    public Vector3 ExtractionPoint { get; }
    public RaidPhase Phase { get; private set; } = RaidPhase.RestorePower;
    public bool ObjectiveRecovered => Phase is RaidPhase.ReturnToSafeBlock or RaidPhase.Extracted;
    public bool PowerRestored => Phase is not RaidPhase.RestorePower;
    public bool IsRaidResolved => Phase is RaidPhase.Extracted or RaidPhase.Failed;
    public bool CriticalMutationActive { get; private set; }
    public bool PlayerInsideInfectedZone { get; private set; }
    public float PlayerMoveScale { get; private set; } = 1f;
    public float ElapsedRaidSeconds { get; private set; }
    public float RoomSizeMultiplier => Sector.RoomSizeMultiplier;
    public float PlayerHealth { get; private set; }

    public float PlayerHealthNormalized =>
        _config.PlayerMaxHealth <= 0.01f ? 0f : PlayerHealth / _config.PlayerMaxHealth;

    public int MedkitCount { get; private set; }
    public float PlayerNoiseLevel { get; private set; }
    public float PlayerVisibilityLevel { get; private set; }
    public bool QuietMovementActive { get; private set; }
    public bool CanUseMedkit => !IsRaidResolved && MedkitCount > 0 && PlayerHealth < _config.PlayerMaxHealth - 0.5f;

    public float TimeRemainingSeconds => MathF.Max(0f, _timeRemainingSeconds);
    public bool CanInteract { get; private set; }
    public bool IgnoreCollision { get; set; }
    public string InteractionPrompt { get; private set; } = string.Empty;
    public string ContextHint { get; private set; } = string.Empty;

    public string ObjectiveLabel => Phase switch
    {
        RaidPhase.RestorePower => "Открыть сервисную дверь и поднять рубильник",
        RaidPhase.ReachObjective => "Пройти к архиву и забрать журналы",
        RaidPhase.ReturnToSafeBlock => "Вернуться к гермоконсоли и закрыть герму",
        RaidPhase.Extracted => "Рейд завершён. Архив вынесен",
        RaidPhase.Failed => "Самосбор накрыл сектор. Рейд сорван",
        _ => string.Empty
    };

    public string PressureLabel => Phase switch
    {
        RaidPhase.Extracted => "Эвакуация завершена",
        RaidPhase.Failed => "Самосбор пришёл",
        _ => PressureLevel switch
        {
            RaidPressureLevel.Stable => "Окно стабильности",
            RaidPressureLevel.Pressure => "Давление нарастает",
            RaidPressureLevel.Critical => "Критическая фаза",
            _ => string.Empty
        }
    };

    public RaidPressureLevel PressureLevel
    {
        get
        {
            if (_timeRemainingSeconds <= _config.CriticalThresholdSeconds) return RaidPressureLevel.Critical;
            if (_timeRemainingSeconds <= _config.PressureThresholdSeconds) return RaidPressureLevel.Pressure;
            return RaidPressureLevel.Stable;
        }
    }

    public WorldRenderable? PowerSwitchMarker => Phase == RaidPhase.RestorePower ? _powerSwitchMarker : null;
    public WorldRenderable? ObjectiveMarker => Phase == RaidPhase.ReachObjective ? _objectiveMarker : null;
    public WorldRenderable? ExtractionMarker => Phase == RaidPhase.ReturnToSafeBlock ? _extractionMarker : null;

    public void Tick(float deltaTime, Vector2 movementInput, bool interactPressed, bool allowCriticalMutation,
        bool quietMovement, bool useMedkit)
    {
        QuietMovementActive = quietMovement;
        if (_contactDamageCooldownSeconds > 0f)
            _contactDamageCooldownSeconds = MathF.Max(0f, _contactDamageCooldownSeconds - deltaTime);

        if (useMedkit) TryUseMedkit();

        if (!IsRaidResolved)
        {
            ElapsedRaidSeconds += deltaTime;
            _timeRemainingSeconds -= deltaTime;
            if (_timeRemainingSeconds <= 0f)
            {
                _timeRemainingSeconds = 0f;
                Phase = RaidPhase.Failed;
                ContextHint = "Сирена сорвалась. Сектор ушёл под самосбор";
            }
        }

        if (!CriticalMutationActive && allowCriticalMutation && !IsRaidResolved &&
            PressureLevel == RaidPressureLevel.Critical) ApplyCriticalMutation();

        if (IsRaidResolved)
        {
            UpdatePlayerEnvironment(0f, Vector2.Zero, quietMovement);
            UpdateInteractionState();
            return;
        }

        MovePlayer(movementInput, deltaTime, quietMovement);
        UpdatePlayerEnvironment(deltaTime, movementInput, quietMovement);
        UpdateHostiles(deltaTime);
        UpdateInteractionState();

        if (interactPressed) TryInteract();
    }

    private void MovePlayer(Vector2 movementInput, float deltaTime, bool quietMovement)
    {
        var movementScale = quietMovement ? _config.QuietWalkMultiplier : 1f;
        var delta = Player.CreateMoveDelta(movementInput, deltaTime * movementScale * PlayerMoveScale);
        if (delta.LengthSquared() <= float.Epsilon) return;

        var current = Player.Transform.Position;
        var candidateX = new Vector3(current.X + delta.X, current.Y, current.Z);
        if (!Collides(candidateX, Player.CollisionRadius)) current = candidateX;

        var candidateZ = new Vector3(current.X, current.Y, current.Z + delta.Z);
        if (!Collides(candidateZ, Player.CollisionRadius)) current = candidateZ;

        current.X = Math.Clamp(current.X, Sector.BoundsMin.X + Player.CollisionRadius,
            Sector.BoundsMax.X - Player.CollisionRadius);
        current.Z = Math.Clamp(current.Z, Sector.BoundsMin.Z + Player.CollisionRadius,
            Sector.BoundsMax.Z - Player.CollisionRadius);
        Player.SetPosition(current);
    }

    private void UpdatePlayerEnvironment(float deltaTime, Vector2 movementInput, bool quietMovement)
    {
        PlayerInsideInfectedZone = false;
        PlayerMoveScale = 1f;
        _playerVisibilityBoost = 0f;
        var infectedDamagePerSecond = 0f;

        foreach (var zone in Sector.InfectedZones)
        {
            if (!IsInfectedZoneActive(zone) || !zone.Contains(Player.Transform.Position)) continue;

            PlayerInsideInfectedZone = true;
            PlayerMoveScale = MathF.Min(PlayerMoveScale, zone.MoveMultiplier);
            infectedDamagePerSecond += zone.DamagePerSecond;
            _playerVisibilityBoost = MathF.Max(_playerVisibilityBoost, zone.VisibilityBoost);
        }

        if (!IsRaidResolved && infectedDamagePerSecond > 0.01f)
            DamagePlayer(infectedDamagePerSecond * _config.InfectedDamageMultiplier * deltaTime,
                "Самосбор прожигает защиту");

        var movementAmount = Math.Clamp(movementInput.Length(), 0f, 1f);
        var targetNoise = movementAmount <= 0.01f ? 0.02f :
            quietMovement ? 0.18f + movementAmount * 0.18f : 0.34f + movementAmount * 0.54f;
        if (PlayerInsideInfectedZone) targetNoise += 0.14f;
        if (CanInteract) targetNoise += 0.02f;
        PlayerNoiseLevel = Damp(PlayerNoiseLevel, Math.Clamp(targetNoise, 0f, 1f), deltaTime, _config.NoiseDecayRate);
        PlayerVisibilityLevel = ComputeLightExposure(Player.Transform.Position);
    }

    private void UpdateHostiles(float deltaTime)
    {
        if (Phase is RaidPhase.Extracted or RaidPhase.Failed) return;

        foreach (var hostile in Sector.Hostiles)
        {
            hostile.Tick(deltaTime, Player.Transform.Position, PlayerNoiseLevel, PlayerVisibilityLevel, PressureLevel,
                PlayerInsideInfectedZone, Collides, HasLineOfSight);

            if (!hostile.CanStrike || _contactDamageCooldownSeconds > 0f) continue;

            var playerXZ = new Vector2(Player.Transform.Position.X, Player.Transform.Position.Z);
            var hostileXZ = new Vector2(hostile.Transform.Position.X, hostile.Transform.Position.Z);
            var hitRadius = hostile.CollisionRadius + Player.CollisionRadius + 0.08f;

            if (Vector2.DistanceSquared(playerXZ, hostileXZ) > hitRadius * hitRadius) continue;

            DamagePlayer(_config.HostileContactDamage, $"{hostile.Label} рвёт дистанцию");
            _contactDamageCooldownSeconds = _config.HostileContactCooldownSeconds;
            EmitNoise(1f);
            if (Phase == RaidPhase.Failed) break;
        }
    }

    private float ComputeLightExposure(Vector3 worldPosition)
    {
        var exposure = 0.08f;
        foreach (var light in Sector.Lights)
        {
            var pressureFactor = PressureLevel switch
            {
                RaidPressureLevel.Stable => 1.00f,
                RaidPressureLevel.Pressure => light.Emergency ? 0.90f : 0.96f,
                RaidPressureLevel.Critical => light.Emergency ? 0.72f : 0.84f,
                _ => 1.00f
            };

            var delta = new Vector2(worldPosition.X - light.Position.X, worldPosition.Z - light.Position.Z);
            var distance = delta.Length();
            if (distance >= light.Radius) continue;

            var distanceFactor = 1f - distance / light.Radius;
            exposure += distanceFactor * light.Intensity * pressureFactor * 0.22f;
        }

        exposure += _playerVisibilityBoost;
        if (ObjectiveRecovered) exposure += 0.06f;
        return Math.Clamp(exposure, 0.04f, 1f);
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        if (IgnoreCollision) return true;

        var start = new Vector2(from.X, from.Z);
        var end = new Vector2(to.X, to.Z);

        foreach (var collider in _solidColliders)
            if (collider.SegmentIntersects(start, end))
                return false;

        foreach (var passage in Sector.LockablePassages.Where(x => x.BlocksPassage))
            if (_passageColliders.TryGetValue(passage.Id, out var collider) && collider.SegmentIntersects(start, end))
                return false;

        if (CriticalMutationActive)
            foreach (var collider in _criticalMutationColliders)
                if (collider.SegmentIntersects(start, end))
                    return false;

        return true;
    }

    private bool Collides(Vector3 worldPosition, float radius)
    {
        if (IgnoreCollision) return false;

        var center = new Vector2(worldPosition.X, worldPosition.Z);
        foreach (var collider in _solidColliders)
            if (collider.IntersectsCircle(center, radius))
                return true;

        foreach (var passage in Sector.LockablePassages.Where(x => x.BlocksPassage))
            if (_passageColliders.TryGetValue(passage.Id, out var collider) &&
                collider.IntersectsCircle(center, radius))
                return true;

        if (CriticalMutationActive)
            foreach (var collider in _criticalMutationColliders)
                if (collider.IntersectsCircle(center, radius))
                    return true;

        return false;
    }

    private void BuildSolidColliders()
    {
        foreach (var renderable in Sector.StaticGeometry) AddCollider(renderable.Transform, _solidColliders);
        foreach (var prop in Sector.Props) AddCollider(prop.Renderable.Transform, _solidColliders);
        foreach (var passage in Sector.LockablePassages)
            AddCollider(passage.Renderable.Transform, _passageColliders, passage.Id);
        foreach (var renderable in Sector.CriticalMutationGeometry)
            AddCollider(renderable.Transform, _criticalMutationColliders);
    }

    private static void AddCollider(Transform transform, ICollection<CollisionBox2D> target)
    {
        if (!ShouldCollide(transform)) return;

        var halfExtents = GetPlanarHalfExtents(transform);
        var min = new Vector2(transform.Position.X - halfExtents.X, transform.Position.Z - halfExtents.Y);
        var max = new Vector2(transform.Position.X + halfExtents.X, transform.Position.Z + halfExtents.Y);
        target.Add(new CollisionBox2D(min, max));
    }

    private static void AddCollider(Transform transform, IDictionary<string, CollisionBox2D> target, string id)
    {
        if (!ShouldCollide(transform)) return;

        var halfExtents = GetPlanarHalfExtents(transform);
        var min = new Vector2(transform.Position.X - halfExtents.X, transform.Position.Z - halfExtents.Y);
        var max = new Vector2(transform.Position.X + halfExtents.X, transform.Position.Z + halfExtents.Y);
        target[id] = new CollisionBox2D(min, max);
    }

    private static bool ShouldCollide(Transform transform)
    {
        if (transform.Scale.Y < 0.3f) return false;

        var bottom = transform.Position.Y - transform.Scale.Y * 0.5f;
        var top = transform.Position.Y + transform.Scale.Y * 0.5f;
        return top > 0.15f && bottom < CollisionCeilingHeight;
    }

    private static Vector2 GetPlanarHalfExtents(Transform transform)
    {
        var halfX = transform.Scale.X * 0.5f;
        var halfZ = transform.Scale.Z * 0.5f;
        var quarterTurns = (int)MathF.Round(transform.Rotation.Y / (MathF.PI * 0.5f));
        var normalizedQuarterTurns = (quarterTurns % 4 + 4) % 4;
        return normalizedQuarterTurns % 2 == 0
            ? new Vector2(halfX, halfZ)
            : new Vector2(halfZ, halfX);
    }

    private void UpdateInteractionState()
    {
        CanInteract = false;
        InteractionPrompt = string.Empty;
        _interactionTarget = InteractionTarget.None;
        _interactionPassageId = null;
        _interactionLootId = null;

        if (IsRaidResolved)
        {
            if (Phase == RaidPhase.Extracted && string.IsNullOrWhiteSpace(ContextHint))
                ContextHint = "Гермодверь закрыта. Сектор остался снаружи";
            else if (Phase == RaidPhase.Failed && string.IsNullOrWhiteSpace(ContextHint))
                ContextHint = "Сектор потерян. Самосбор закрыл рейд";
            return;
        }

        ContextHint = string.Empty;

        if (Phase == RaidPhase.RestorePower && IsWithinInteractRange(PowerSwitchPoint))
        {
            CanInteract = true;
            InteractionPrompt = "E — поднять рубильник и снять блокировку с аварийной переборки";
            _interactionTarget = InteractionTarget.PowerSwitch;
            return;
        }

        if (Phase == RaidPhase.ReachObjective && IsWithinInteractRange(ObjectivePoint))
        {
            CanInteract = true;
            InteractionPrompt = "E — забрать архивные журналы";
            _interactionTarget = InteractionTarget.Objective;
            return;
        }

        if (Phase == RaidPhase.ReturnToSafeBlock && IsWithinInteractRange(ExtractionPoint))
        {
            CanInteract = true;
            InteractionPrompt = "E — закрыть герму и завершить рейд";
            _interactionTarget = InteractionTarget.Extraction;
            return;
        }

        foreach (var pickup in Sector.Loot.Where(x => !x.Collected))
        {
            if (!IsWithinInteractRange(pickup.Position)) continue;

            CanInteract = true;
            InteractionPrompt = $"E — подобрать {pickup.Label.ToLowerInvariant()}";
            _interactionTarget = InteractionTarget.Loot;
            _interactionLootId = pickup.Id;
            return;
        }

        foreach (var passage in Sector.LockablePassages)
        {
            if (!passage.Visible || !IsWithinInteractRange(passage.Renderable.Transform.Position)) continue;

            switch (passage.State)
            {
                case DoorState.Closed:
                    CanInteract = true;
                    InteractionPrompt = $"E — открыть {passage.Label.ToLowerInvariant()}";
                    _interactionTarget = InteractionTarget.Passage;
                    _interactionPassageId = passage.Id;
                    return;
                case DoorState.Locked:
                    ContextHint = "Проход заперт. Нужно восстановить питание";
                    return;
                case DoorState.Jammed:
                    ContextHint = "Проход заклинило. Его режет самосбор";
                    return;
            }
        }

        if (PlayerInsideInfectedZone)
        {
            var zone = Sector.InfectedZones.FirstOrDefault(x =>
                IsInfectedZoneActive(x) && x.Contains(Player.Transform.Position));
            if (zone is not null)
            {
                ContextHint = zone.Label;
                return;
            }
        }

        var nearestAlert = Sector.Hostiles
            .Where(x => x.Alerted)
            .OrderBy(x => Vector2.DistanceSquared(
                new Vector2(x.Transform.Position.X, x.Transform.Position.Z),
                new Vector2(Player.Transform.Position.X, Player.Transform.Position.Z)))
            .FirstOrDefault();

        if (nearestAlert is not null)
            ContextHint = nearestAlert.AwarenessState switch
            {
                HostileAwarenessState.Hunt => $"{nearestAlert.Label} идёт по следу",
                HostileAwarenessState.Suspicious => $"{nearestAlert.Label} услышал движение",
                HostileAwarenessState.Search => $"{nearestAlert.Label} шарит по коридору",
                _ => ContextHint
            };
    }

    private void TryInteract()
    {
        if (!CanInteract) return;

        switch (_interactionTarget)
        {
            case InteractionTarget.PowerSwitch:
                foreach (var passage in Sector.LockablePassages.Where(x =>
                             x.UnlockId.Equals("service_power", StringComparison.OrdinalIgnoreCase)))
                    passage.Unlock();
                Phase = RaidPhase.ReachObjective;
                ContextHint = "Питание вернулось. Переборка к архиву разблокирована";
                EmitNoise(0.60f);
                break;
            case InteractionTarget.Objective:
                Phase = RaidPhase.ReturnToSafeBlock;
                ContextHint = "Журналы в сумке. Теперь назад к герме";
                EmitNoise(0.38f);
                break;
            case InteractionTarget.Extraction:
                Phase = RaidPhase.Extracted;
                ContextHint = "Герма закрылась. Рейд пережит";
                break;
            case InteractionTarget.Passage:
                if (_interactionPassageId is not null)
                {
                    Sector.LockablePassages
                        .FirstOrDefault(x => x.Id.Equals(_interactionPassageId, StringComparison.OrdinalIgnoreCase))
                        ?.Open();
                    ContextHint = "Тяжёлая дверь ушла в сторону";
                    EmitNoise(0.66f);
                }

                break;
            case InteractionTarget.Loot:
                if (_interactionLootId is not null)
                {
                    var pickup = Sector.Loot.FirstOrDefault(x =>
                        x.Id.Equals(_interactionLootId, StringComparison.OrdinalIgnoreCase));
                    if (pickup is not null && !pickup.Collected)
                    {
                        pickup.Collect();
                        if (pickup.MedkitCount > 0)
                        {
                            MedkitCount += pickup.MedkitCount;
                            ContextHint = $"В сумку ушло: {pickup.Label.ToLowerInvariant()}";
                        }
                        else
                        {
                            ContextHint = $"Подобрано: {pickup.Label.ToLowerInvariant()}";
                        }

                        EmitNoise(0.48f);
                    }
                }

                break;
        }

        UpdateInteractionState();
    }

    private void TryUseMedkit()
    {
        if (!CanUseMedkit) return;

        MedkitCount--;
        PlayerHealth = MathF.Min(_config.PlayerMaxHealth, PlayerHealth + _config.MedkitHealAmount);
        ContextHint = "Аптечка стабилизировала состояние";
        PlayerNoiseLevel = MathF.Max(PlayerNoiseLevel, 0.10f);
    }

    private void DamagePlayer(float amount, string hint)
    {
        if (amount <= 0.01f || IsRaidResolved) return;

        PlayerHealth = MathF.Max(0f, PlayerHealth - amount);
        ContextHint = hint;
        if (PlayerHealth <= 0f)
        {
            Phase = RaidPhase.Failed;
            ContextHint = "Игрок не пережил рейд";
        }
    }

    private void ApplyCriticalMutation()
    {
        CriticalMutationActive = true;
        foreach (var passage in Sector.LockablePassages.Where(x => x.ActivateOnCriticalPhase)) passage.Jam();
        ContextHint = "Сектор перестраивается. Самосбор режет боковые маршруты";
    }

    private bool IsWithinInteractRange(Vector3 worldPoint)
    {
        var playerXZ = new Vector2(Player.Transform.Position.X, Player.Transform.Position.Z);
        var pointXZ = new Vector2(worldPoint.X, worldPoint.Z);
        return Vector2.DistanceSquared(playerXZ, pointXZ) <= _config.InteractRadius * _config.InteractRadius;
    }

    private bool IsInfectedZoneActive(InfectedZone zone)
    {
        return zone.IsActive(PressureLevel) ||
               (CriticalMutationActive && zone.ActivationLevel == RaidPressureLevel.Critical);
    }

    private void EmitNoise(float amount)
    {
        PlayerNoiseLevel = MathF.Max(PlayerNoiseLevel, Math.Clamp(amount, 0f, 1f));
    }

    private static float Damp(float current, float target, float deltaTime, float response)
    {
        if (deltaTime <= 0f) return target;
        var alpha = 1f - MathF.Exp(-MathF.Max(0.01f, response) * deltaTime);
        return current + (target - current) * alpha;
    }

    private enum InteractionTarget
    {
        None,
        PowerSwitch,
        Objective,
        Extraction,
        Passage,
        Loot
    }
}
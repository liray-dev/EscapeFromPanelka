using System.Numerics;
using EFP.Model.Common;
using EFP.Model.Raid.Objectives;
using EFP.Model.Raid.Props;
using EFP.Model.WorldGen;

namespace EFP.Model.Raid;

public sealed class Raid
{
    private const float CollisionCeilingHeight = 1.85f;
    private readonly RaidSettings _config;
    private readonly List<RaidObjective> _objectives =
    [
        new RestorePowerObjective(),
        new RecoverArchiveObjective(),
        new ReachExtractionObjective()
    ];
    private readonly List<CollisionBox2D> _criticalMutationColliders = [];
    private readonly WorldRenderable _extractionMarker;
    private readonly WorldRenderable _objectiveMarker;
    private readonly Dictionary<string, CollisionBox2D> _passageColliders = new(StringComparer.OrdinalIgnoreCase);
    private readonly WorldRenderable _powerSwitchMarker;
    private readonly List<CollisionBox2D> _solidColliders = [];
    private float _contactDamageCooldownSeconds;
    private Container? _activeSearchContainer;
    private string? _interactionContainerId;
    private string? _interactionPassageId;
    private InteractionTarget _interactionTarget;
    private string _contextHint = string.Empty;
    private RaidPhase _phase = RaidPhase.RestorePower;
    private float _playerHealth;
    private float _playerVisibilityBoost;
    private int _medkitCount;
    private float _timeRemainingSeconds;

    public event EventHandler<PlayerHealthChanged>? PlayerHealthChanged;
    public event EventHandler<MedkitsChanged>? MedkitsChanged;
    public event EventHandler<PhaseChanged>? PhaseChanged;
    public event EventHandler<LootCollected>? LootCollected;
    public event EventHandler<HostileDied>? HostileDied;
    public event EventHandler<AttackPerformed>? AttackPerformed;
    public event EventHandler<ContextHintChanged>? ContextHintChanged;
    public event EventHandler<RaidEnded>? RaidEnded;
    public event EventHandler<ContainerSearchStarted>? ContainerSearchStarted;
    public event EventHandler<ContainerSearchProgress>? ContainerSearchProgress;
    public event EventHandler<ContainerSearchCancelled>? ContainerSearchCancelled;
    public event EventHandler<ContainerOpened>? ContainerOpened;

    public Raid(RaidSettings config, ProceduralSector sector)
    {
        _config = config;
        Sector = sector;
        Player = new Player(config.PlayerMoveSpeed, config.PlayerCollisionRadius, sector.PlayerSpawn);
        Combat = new PlayerCombat(config, sector.Seed);
        PowerSwitchPoint = sector.PowerSwitchPoint;
        ObjectivePoint = sector.ObjectivePoint;
        ExtractionPoint = sector.ExtractionConsolePoint;
        _timeRemainingSeconds = config.RaidDurationSeconds;
        _playerHealth = config.PlayerMaxHealth;
        _medkitCount = Math.Max(0, config.StartingMedkits);

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

        var safeModule = FindModuleByArchetype("safe");
        var serviceModule = FindModuleByArchetype("service");
        var objectiveModule = FindModuleByArchetype("objective");

        _powerSwitchMarker = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = PowerSwitchPoint + new Vector3(0f, 0.55f, 0f),
                Scale = new Vector3(0.42f, 1.1f, 0.42f)
            },
            new Vector4(0.84f, 0.70f, 0.24f, 1f),
            ownerModuleId: serviceModule?.NodeId);

        _objectiveMarker = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = ObjectivePoint + new Vector3(0f, 0.55f, 0f),
                Scale = new Vector3(0.55f, 1.1f, 0.55f)
            },
            new Vector4(0.16f, 0.72f, 0.64f, 1f),
            ownerModuleId: objectiveModule?.NodeId);

        _extractionMarker = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = ExtractionPoint + new Vector3(0f, 0.55f, 0f),
                Scale = new Vector3(0.55f, 1.1f, 0.55f)
            },
            new Vector4(0.84f, 0.71f, 0.22f, 1f),
            ownerModuleId: safeModule?.NodeId);

        BuildSolidColliders();
        BuildExtractionPoints();
        UpdatePlayerEnvironment(0f, Vector2.Zero, false);
        UpdateModuleVisibility();
        UpdateInteractionState();
    }

    public ModuleVisibility GetVisibility(string? ownerModuleId)
    {
        if (string.IsNullOrEmpty(ownerModuleId)) return ModuleVisibility.Visible;
        if (!_moduleByNodeId.TryGetValue(ownerModuleId, out var module)) return ModuleVisibility.Visible;
        if (module.Visible) return ModuleVisibility.Visible;
        return module.Discovered ? ModuleVisibility.Discovered : ModuleVisibility.Hidden;
    }

    private void UpdateModuleVisibility()
    {
        if (_moduleByNodeId.Count == 0)
        {
            foreach (var module in Sector.Modules)
                _moduleByNodeId[module.NodeId] = module;
        }

        foreach (var module in Sector.Modules) module.Visible = false;

        var playerPos = Player.Transform.Position;
        var startModule = FindModuleContaining(playerPos) ?? FindNearestModule(playerPos);
        if (startModule is null) return;

        var queue = new Queue<PlacedModule>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        queue.Enqueue(startModule);
        visited.Add(startModule.NodeId);

        while (queue.Count > 0)
        {
            var module = queue.Dequeue();
            module.Visible = true;
            module.Discovered = true;

            if (!Sector.ModuleAdjacency.TryGetValue(module.NodeId, out var neighbors)) continue;

            foreach (var link in neighbors)
            {
                if (!visited.Add(link.NeighborNodeId)) continue;
                if (!IsPassageRevealing(link.PassageId)) continue;
                if (!_moduleByNodeId.TryGetValue(link.NeighborNodeId, out var neighbor)) continue;
                queue.Enqueue(neighbor);
            }
        }
    }

    private bool IsPassageRevealing(string passageId)
    {
        var passage = Sector.LockablePassages.FirstOrDefault(p =>
            p.Id.Equals(passageId, StringComparison.OrdinalIgnoreCase));
        if (passage is null) return true;
        return passage.State == DoorState.Open;
    }

    private PlacedModule? FindModuleByArchetype(string archetype)
    {
        return Sector.Modules.FirstOrDefault(m =>
            m.Definition.Archetype.Equals(archetype, StringComparison.OrdinalIgnoreCase));
    }

    private PlacedModule? FindModuleContaining(Vector3 worldPosition)
    {
        foreach (var module in Sector.Modules)
            if (module.ContainsPlanar(worldPosition))
                return module;
        return null;
    }

    private PlacedModule? FindNearestModule(Vector3 worldPosition)
    {
        var xz = new Vector2(worldPosition.X, worldPosition.Z);
        PlacedModule? best = null;
        var bestDistance = float.MaxValue;
        foreach (var module in Sector.Modules)
        {
            var moduleXZ = new Vector2(module.Position.X, module.Position.Z);
            var distance = Vector2.DistanceSquared(xz, moduleXZ);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = module;
        }

        return best;
    }

    private readonly Dictionary<string, PlacedModule> _moduleByNodeId = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<ExtractionPoint> _extractionPoints = [];
    private string? _interactionExtractionId;

    private void BuildExtractionPoints()
    {
        var rng = new Random(Sector.Seed ^ 0x4A5E5A5A);
        var candidates = Sector.Modules
            .Where(m => !m.Definition.Archetype.Equals("safe", StringComparison.OrdinalIgnoreCase)
                        && !m.Definition.Archetype.Equals("objective", StringComparison.OrdinalIgnoreCase)
                        && !m.Definition.Archetype.Equals("service", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => Vector3.DistanceSquared(m.Position, Sector.SafeBlockCenter))
            .Take(Math.Min(12, Sector.Modules.Count))
            .ToList();
        if (candidates.Count == 0) return;

        var picks = new List<PlacedModule>();
        for (var i = 0; i < 3 && candidates.Count > 0; i++)
        {
            var index = rng.Next(candidates.Count);
            picks.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        for (var i = 0; i < picks.Count; i++)
        {
            var module = picks[i];
            var position = module.Position + new Vector3(0f, 0.95f, 0f);
            var condition = BuildRandomObjective(rng);
            var label = $"Радиальный выход #{i + 1}";
            var markerTint = new Vector4(0.94f, 0.78f, 0.22f, 1f);
            var marker = new WorldRenderable(
                WorldPrimitiveType.Cube,
                new Transform
                {
                    Position = position + new Vector3(0f, 0.20f, 0f),
                    Scale = new Vector3(0.36f, 1.40f, 0.36f)
                },
                markerTint,
                ownerModuleId: module.NodeId);

            _extractionPoints.Add(new ExtractionPoint($"extract_{i}_{module.NodeId}", label, position, condition, marker));
        }
    }

    private static Objectives.RaidObjective BuildRandomObjective(Random rng)
    {
        var roll = rng.NextDouble();
        if (roll < 0.40)
        {
            var target = rng.Next(3, 9);
            return new Objectives.KillHostilesObjective(target);
        }

        if (roll < 0.75)
        {
            var kinds = new[]
            {
                (LootKind.Scrap, "лом", 3),
                (LootKind.Filter, "фильтры", 2),
                (LootKind.Battery, "батареи", 2),
                (LootKind.Reagent, "реагенты", 1)
            };
            var pick = kinds[rng.Next(kinds.Length)];
            return new Objectives.FetchLootObjective(pick.Item1, pick.Item3, pick.Item2);
        }

        return new Objectives.ReachOnlyObjective();
    }

    public ProceduralSector Sector { get; }
    public Player Player { get; }
    public PlayerCombat Combat { get; }
    public WorldRenderable Foundation { get; }
    public IReadOnlyList<WorldRenderable> StaticGeometry => Sector.StaticGeometry;
    public IReadOnlyList<WorldRenderable> FeatureGeometry => Sector.FeatureGeometry;
    public IReadOnlyList<WorldLight> Lights => Sector.Lights;
    public IReadOnlyList<Hostile> Hostiles => Sector.Hostiles;
    public IEnumerable<Hostile> AliveHostiles => Sector.Hostiles.Where(x => !x.IsDead);
    public int AliveHostileCount => Sector.Hostiles.Count(x => !x.IsDead);
    public int HostileKillCount => Sector.Hostiles.Count(x => x.IsDead);
    public IEnumerable<LootPickup> ActiveLootPickups => Sector.Loot.Where(x => !x.Collected);
    public IReadOnlyList<Container> Containers => Sector.Containers;
    public IReadOnlyList<ExtractionPoint> ExtractionPoints => _extractionPoints;

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
    public RaidPhase Phase => _phase;
    public bool ObjectiveRecovered => _phase is RaidPhase.ReturnToSafeBlock or RaidPhase.Extracted;
    public bool PowerRestored => _phase is not RaidPhase.RestorePower;
    public bool IsRaidResolved => _phase is RaidPhase.Extracted or RaidPhase.Failed;
    public bool CriticalMutationActive { get; private set; }
    public bool PlayerInsideInfectedZone { get; private set; }
    public float PlayerMoveScale { get; private set; } = 1f;
    public float ElapsedRaidSeconds { get; private set; }
    public float RoomSizeMultiplier => Sector.RoomSizeMultiplier;
    public float PlayerHealth => _playerHealth;
    public float PlayerMaxHealth => _config.PlayerMaxHealth;

    public float PlayerHealthNormalized =>
        _config.PlayerMaxHealth <= 0.01f ? 0f : _playerHealth / _config.PlayerMaxHealth;

    public int MedkitCount => _medkitCount;
    public float PlayerNoiseLevel { get; private set; }
    public float PlayerVisibilityLevel { get; private set; }
    public bool QuietMovementActive { get; private set; }
    public bool CanUseMedkit => !IsRaidResolved && _medkitCount > 0 && _playerHealth < _config.PlayerMaxHealth - 0.5f;

    public float TimeRemainingSeconds => MathF.Max(0f, _timeRemainingSeconds);
    public float RaidDurationSeconds => _config.RaidDurationSeconds;
    public bool CanInteract { get; private set; }
    public bool IgnoreCollision { get; set; }
    public string InteractionPrompt { get; private set; } = string.Empty;
    public string ContextHint => _contextHint;
    public float AttackCooldownSeconds => _config.AttackCooldownSeconds;

    public IReadOnlyList<RaidObjective> Objectives => _objectives;
    public RaidObjective? CurrentObjective => _objectives.FirstOrDefault(o => !o.IsComplete(this));

    public string ObjectiveLabel => _phase switch
    {
        RaidPhase.Extracted => "Рейд завершён. Архив вынесен",
        RaidPhase.Failed => "Самосбор накрыл сектор. Рейд сорван",
        _ => CurrentObjective?.Describe(this) ?? string.Empty
    };

    public string PressureLabel => _phase switch
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

    public WorldRenderable? PowerSwitchMarker => _phase == RaidPhase.RestorePower ? _powerSwitchMarker : null;
    public WorldRenderable? ObjectiveMarker => _phase == RaidPhase.ReachObjective ? _objectiveMarker : null;
    public WorldRenderable? ExtractionMarker => _phase == RaidPhase.ReturnToSafeBlock ? _extractionMarker : null;

    public void Tick(float deltaTime, Vector2 movementInput, bool interactPressed, bool allowCriticalMutation,
        bool quietMovement, bool useMedkit, bool attackHeld)
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
                SetPhase(RaidPhase.Failed);
                SetContextHint("Сирена сорвалась. Сектор ушёл под самосбор");
            }
        }

        if (!CriticalMutationActive && allowCriticalMutation && !IsRaidResolved &&
            PressureLevel == RaidPressureLevel.Critical) ApplyCriticalMutation();

        if (IsRaidResolved)
        {
            Combat.Tick(deltaTime, false);
            UpdatePlayerEnvironment(0f, Vector2.Zero, quietMovement);
            UpdateInteractionState();
            return;
        }

        MovePlayer(movementInput, deltaTime, quietMovement);
        UpdatePlayerEnvironment(deltaTime, movementInput, quietMovement);
        UpdateModuleVisibility();

        var attack = Combat.Tick(deltaTime, attackHeld);
        if (attack is { } resolved) ResolveAttack(resolved);

        UpdateHostiles(deltaTime);
        AdvanceContainerSearch(deltaTime);
        UpdateInteractionState();

        if (interactPressed) TryInteract();
    }

    private void AdvanceContainerSearch(float deltaTime)
    {
        if (_activeSearchContainer is null) return;

        if (_activeSearchContainer.State != ContainerState.Searching)
        {
            _activeSearchContainer = null;
            return;
        }

        if (!IsWithinInteractRange(_activeSearchContainer.Position))
        {
            var cancelled = _activeSearchContainer;
            _activeSearchContainer = null;
            cancelled.CancelSearch();
            SetContextHint("Обыск прерван");
            ContainerSearchCancelled?.Invoke(this, new ContainerSearchCancelled(cancelled));
            return;
        }

        if (_activeSearchContainer.Advance(deltaTime))
        {
            var opened = _activeSearchContainer;
            _activeSearchContainer = null;
            opened.ConfirmLooted();
            LootCollected?.Invoke(this, new LootCollected(opened.Loot));
            if (opened.Loot.MedkitCount > 0)
            {
                SetMedkits(_medkitCount + opened.Loot.MedkitCount);
                SetContextHint($"В сумку ушло: {opened.Loot.Label.ToLowerInvariant()}");
            }
            else
            {
                SetContextHint($"Подобрано: {opened.Loot.Label.ToLowerInvariant()}");
            }

            EmitNoise(0.32f);
            ContainerOpened?.Invoke(this, new ContainerOpened(opened));
        }
        else
        {
            ContainerSearchProgress?.Invoke(this,
                new ContainerSearchProgress(_activeSearchContainer, _activeSearchContainer.NormalizedProgress));
        }
    }

    private void ResolveAttack(AttackResult attack)
    {
        var origin = Player.Transform.Position;
        var forward = new Vector2(MathF.Sin(Player.YawRadians), MathF.Cos(Player.YawRadians));
        if (forward.LengthSquared() <= 0.0001f) forward = Vector2.UnitY;

        var halfFovCos = MathF.Cos(MathF.PI / 180f * _config.AttackHalfFovDegrees);
        var hitsLanded = 0;
        Hostile? killed = null;

        foreach (var hostile in Sector.Hostiles)
        {
            if (hostile.IsDead) continue;
            var toHostile = new Vector2(hostile.Transform.Position.X - origin.X,
                hostile.Transform.Position.Z - origin.Z);
            var distanceSquared = toHostile.LengthSquared();
            var effectiveRange = _config.AttackRange + hostile.CollisionRadius;
            if (distanceSquared > effectiveRange * effectiveRange) continue;
            if (distanceSquared <= 0.0001f)
            {
                hostile.OnHit(attack.Damage, origin);
                hitsLanded++;
                if (hostile.IsDead) killed = hostile;
                continue;
            }

            var direction = toHostile / MathF.Sqrt(distanceSquared);
            var contactRange = hostile.CollisionRadius + Player.CollisionRadius + 0.12f;
            if (distanceSquared > contactRange * contactRange && Vector2.Dot(forward, direction) < halfFovCos)
                continue;

            hostile.OnHit(attack.Damage, origin);
            hitsLanded++;
            if (hostile.IsDead) killed = hostile;
        }

        EmitNoise(attack.Critical ? _config.AttackCriticalNoise : _config.AttackNoise);
        AttackPerformed?.Invoke(this, new AttackPerformed(attack, hitsLanded, origin,
            new Vector3(forward.X, 0f, forward.Y), _config.AttackRange));

        if (killed is not null)
        {
            HostileDied?.Invoke(this, new HostileDied(killed));
            SetContextHint(attack.Critical
                ? $"Критический: {killed.Label.ToLowerInvariant()} повержен"
                : $"{killed.Label} повержен");
        }
        else if (hitsLanded > 0)
        {
            SetContextHint(attack.Critical ? "Критический удар лома" : "Удар лома попадает");
        }
        else if (attack.Critical)
        {
            SetContextHint("Критический удар уходит в пустоту");
        }
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
        if (_phase is RaidPhase.Extracted or RaidPhase.Failed) return;

        foreach (var hostile in Sector.Hostiles)
        {
            hostile.Tick(deltaTime, Player.Transform.Position, PlayerNoiseLevel, PlayerVisibilityLevel, PressureLevel,
                PlayerInsideInfectedZone, HostileCollides, HasLineOfSight);

            if (!hostile.CanStrike || _contactDamageCooldownSeconds > 0f) continue;

            var playerXZ = new Vector2(Player.Transform.Position.X, Player.Transform.Position.Z);
            var hostileXZ = new Vector2(hostile.Transform.Position.X, hostile.Transform.Position.Z);
            var hitRadius = hostile.CollisionRadius + Player.CollisionRadius + 0.08f;

            if (Vector2.DistanceSquared(playerXZ, hostileXZ) > hitRadius * hitRadius) continue;

            DamagePlayer(_config.HostileContactDamage, $"{hostile.Label} рвёт дистанцию");
            _contactDamageCooldownSeconds = _config.HostileContactCooldownSeconds;
            EmitNoise(1f);
            if (_phase == RaidPhase.Failed) break;
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

    private bool HostileCollides(Vector3 worldPosition, float radius)
    {
        if (Collides(worldPosition, radius)) return true;

        var hostileXZ = new Vector2(worldPosition.X, worldPosition.Z);
        var playerXZ = new Vector2(Player.Transform.Position.X, Player.Transform.Position.Z);
        var minDistance = radius + Player.CollisionRadius + 0.04f;
        return Vector2.DistanceSquared(hostileXZ, playerXZ) < minDistance * minDistance;
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
        _interactionContainerId = null;
        _interactionExtractionId = null;

        if (IsRaidResolved)
        {
            if (_phase == RaidPhase.Extracted && string.IsNullOrWhiteSpace(_contextHint))
                SetContextHint("Гермодверь закрыта. Сектор остался снаружи");
            else if (_phase == RaidPhase.Failed && string.IsNullOrWhiteSpace(_contextHint))
                SetContextHint("Сектор потерян. Самосбор закрыл рейд");
            return;
        }

        SetContextHint(string.Empty);

        if (_phase == RaidPhase.RestorePower && IsWithinInteractRange(PowerSwitchPoint))
        {
            CanInteract = true;
            InteractionPrompt = "E — поднять рубильник и снять блокировку с аварийной переборки";
            _interactionTarget = InteractionTarget.PowerSwitch;
            return;
        }

        if (_phase == RaidPhase.ReachObjective && IsWithinInteractRange(ObjectivePoint))
        {
            CanInteract = true;
            InteractionPrompt = "E — забрать архивные журналы";
            _interactionTarget = InteractionTarget.Objective;
            return;
        }

        if (_phase == RaidPhase.ReturnToSafeBlock && IsWithinInteractRange(ExtractionPoint))
        {
            CanInteract = true;
            InteractionPrompt = "E — закрыть герму и завершить рейд";
            _interactionTarget = InteractionTarget.Extraction;
            return;
        }

        foreach (var extract in _extractionPoints)
        {
            if (extract.Used) continue;
            if (!IsWithinInteractRange(extract.Position)) continue;

            CanInteract = true;
            var done = extract.Condition.IsComplete(this);
            InteractionPrompt = done
                ? $"E — выйти через {extract.Label.ToLowerInvariant()}"
                : $"Условие не выполнено: {extract.Condition.Describe(this)}";
            _interactionTarget = done ? InteractionTarget.RadialExtract : InteractionTarget.None;
            _interactionExtractionId = done ? extract.Id : null;
            if (!done) CanInteract = false;
            return;
        }

        foreach (var container in Sector.Containers)
        {
            if (container.State == ContainerState.Looted) continue;
            if (!IsWithinInteractRange(container.Position)) continue;

            if (container.State == ContainerState.Searching)
            {
                SetContextHint($"Обыск: {container.Loot.Label.ToLowerInvariant()} ({container.NormalizedProgress * 100f:0}%)");
                return;
            }

            CanInteract = true;
            InteractionPrompt = container.GetInteractionPrompt();
            _interactionTarget = InteractionTarget.Container;
            _interactionContainerId = container.Id;
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
                    SetContextHint("Проход заперт. Нужно восстановить питание");
                    return;
                case DoorState.Jammed:
                    SetContextHint("Проход заклинило. Его режет самосбор");
                    return;
            }
        }

        if (PlayerInsideInfectedZone)
        {
            var zone = Sector.InfectedZones.FirstOrDefault(x =>
                IsInfectedZoneActive(x) && x.Contains(Player.Transform.Position));
            if (zone is not null)
            {
                SetContextHint(zone.Label);
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
            SetContextHint(nearestAlert.AwarenessState switch
            {
                HostileAwarenessState.Hunt => $"{nearestAlert.Label} идёт по следу",
                HostileAwarenessState.Suspicious => $"{nearestAlert.Label} услышал движение",
                HostileAwarenessState.Search => $"{nearestAlert.Label} шарит по коридору",
                _ => _contextHint
            });
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
                SetPhase(RaidPhase.ReachObjective);
                SetContextHint("Питание вернулось. Переборка к архиву разблокирована");
                EmitNoise(0.60f);
                break;
            case InteractionTarget.Objective:
                SetPhase(RaidPhase.ReturnToSafeBlock);
                SetContextHint("Журналы в сумке. Теперь назад к герме");
                EmitNoise(0.38f);
                break;
            case InteractionTarget.Extraction:
                SetPhase(RaidPhase.Extracted);
                SetContextHint("Герма закрылась. Рейд пережит");
                break;
            case InteractionTarget.Passage:
                if (_interactionPassageId is not null)
                {
                    Sector.LockablePassages
                        .FirstOrDefault(x => x.Id.Equals(_interactionPassageId, StringComparison.OrdinalIgnoreCase))
                        ?.Open();
                    SetContextHint("Тяжёлая дверь ушла в сторону");
                    EmitNoise(0.66f);
                }

                break;
            case InteractionTarget.RadialExtract:
                if (_interactionExtractionId is not null)
                {
                    var extract = _extractionPoints.FirstOrDefault(x =>
                        x.Id.Equals(_interactionExtractionId, StringComparison.OrdinalIgnoreCase));
                    if (extract is not null && !extract.Used && extract.Condition.IsComplete(this))
                    {
                        extract.MarkUsed();
                        SetPhase(RaidPhase.Extracted);
                        SetContextHint($"Выход через {extract.Label.ToLowerInvariant()}: рейд завершён");
                        EmitNoise(0.55f);
                    }
                }

                break;
            case InteractionTarget.Container:
                if (_interactionContainerId is not null)
                {
                    var container = Sector.Containers.FirstOrDefault(c =>
                        c.Id.Equals(_interactionContainerId, StringComparison.OrdinalIgnoreCase));
                    if (container is not null && container.StartSearch())
                    {
                        _activeSearchContainer = container;
                        SetContextHint($"Обыск: {container.Loot.Label.ToLowerInvariant()}");
                        EmitNoise(0.18f);
                        ContainerSearchStarted?.Invoke(this, new ContainerSearchStarted(container));
                    }
                }

                break;
        }

        UpdateInteractionState();
    }

    private void TryUseMedkit()
    {
        if (!CanUseMedkit) return;

        SetMedkits(_medkitCount - 1);
        SetPlayerHealth(MathF.Min(_config.PlayerMaxHealth, _playerHealth + _config.MedkitHealAmount));
        SetContextHint("Аптечка стабилизировала состояние");
        PlayerNoiseLevel = MathF.Max(PlayerNoiseLevel, 0.10f);
    }

    private void DamagePlayer(float amount, string hint)
    {
        if (amount <= 0.01f || IsRaidResolved) return;

        SetPlayerHealth(MathF.Max(0f, _playerHealth - amount));
        SetContextHint(hint);
        if (_playerHealth <= 0f)
        {
            SetPhase(RaidPhase.Failed);
            SetContextHint("Игрок не пережил рейд");
        }
    }

    private void ApplyCriticalMutation()
    {
        CriticalMutationActive = true;
        foreach (var passage in Sector.LockablePassages.Where(x => x.ActivateOnCriticalPhase)) passage.Jam();
        SetContextHint("Сектор перестраивается. Самосбор режет боковые маршруты");
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

    private void SetPhase(RaidPhase phase)
    {
        if (_phase == phase) return;
        _phase = phase;
        PhaseChanged?.Invoke(this, new PhaseChanged(phase));
        if (phase is RaidPhase.Extracted or RaidPhase.Failed)
            RaidEnded?.Invoke(this, new RaidEnded(phase, CargoCount, CargoValue));
    }

    private void SetPlayerHealth(float value)
    {
        if (MathF.Abs(_playerHealth - value) < 0.0001f) return;
        _playerHealth = value;
        PlayerHealthChanged?.Invoke(this, new PlayerHealthChanged(_playerHealth, _config.PlayerMaxHealth));
    }

    private void SetMedkits(int value)
    {
        if (_medkitCount == value) return;
        _medkitCount = value;
        MedkitsChanged?.Invoke(this, new MedkitsChanged(_medkitCount));
    }

    private void SetContextHint(string hint)
    {
        if (string.Equals(_contextHint, hint, StringComparison.Ordinal)) return;
        _contextHint = hint;
        ContextHintChanged?.Invoke(this, new ContextHintChanged(_contextHint));
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
        Container,
        RadialExtract
    }
}

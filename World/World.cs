using System.Numerics;
using EFP.App;
using EFP.Entities;
using EFP.WorldGen;

namespace EFP.World;

public sealed class World
{
    private readonly GameplayConfig _config;
    private readonly List<CollisionBox2D> _solidColliders = [];
    private readonly Dictionary<string, CollisionBox2D> _lockablePassageColliders = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unlockedPassages = new(StringComparer.OrdinalIgnoreCase);
    private readonly WorldRenderable _powerSwitchMarker;
    private readonly WorldRenderable _objectiveMarker;
    private readonly WorldRenderable _extractionMarker;
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
        UpdateInteractionState();
    }

    public ProceduralSector Sector { get; }
    public PlayerCube Player { get; }
    public WorldRenderable Foundation { get; }
    public IReadOnlyList<WorldRenderable> StaticGeometry => Sector.StaticGeometry;
    public IReadOnlyList<WorldRenderable> FeatureGeometry => Sector.FeatureGeometry;
    public IReadOnlyList<PropInstance> Props => Sector.Props;
    public IEnumerable<LockablePassage> ActiveLockablePassages => Sector.LockablePassages.Where(x => !_unlockedPassages.Contains(x.Id));
    public int Seed => Sector.Seed;
    public int ModuleCount => Sector.Modules.Count;
    public int PropCount => Sector.Props.Count;
    public int LockedPassageCount => ActiveLockablePassages.Count();
    public Vector3 PowerSwitchPoint { get; }
    public Vector3 ObjectivePoint { get; }
    public Vector3 ExtractionPoint { get; }
    public RaidPhase Phase { get; private set; } = RaidPhase.RestorePower;
    public bool ObjectiveRecovered => Phase is RaidPhase.ReturnToSafeBlock or RaidPhase.Extracted;
    public bool PowerRestored => Phase is not RaidPhase.RestorePower;
    public bool IsRaidResolved => Phase is RaidPhase.Extracted or RaidPhase.Failed;
    public float TimeRemainingSeconds => MathF.Max(0f, _timeRemainingSeconds);
    public bool CanInteract { get; private set; }
    public string InteractionPrompt { get; private set; } = string.Empty;
    public string ContextHint { get; private set; } = string.Empty;
    public string ObjectiveLabel => Phase switch
    {
        RaidPhase.RestorePower => "Найти service nook и поднять рубильник",
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
            if (_timeRemainingSeconds <= _config.CriticalThresholdSeconds)
            {
                return RaidPressureLevel.Critical;
            }

            if (_timeRemainingSeconds <= _config.PressureThresholdSeconds)
            {
                return RaidPressureLevel.Pressure;
            }

            return RaidPressureLevel.Stable;
        }
    }

    public WorldRenderable? PowerSwitchMarker => Phase == RaidPhase.RestorePower ? _powerSwitchMarker : null;
    public WorldRenderable? ObjectiveMarker => Phase == RaidPhase.ReachObjective ? _objectiveMarker : null;
    public WorldRenderable? ExtractionMarker => Phase == RaidPhase.ReturnToSafeBlock ? _extractionMarker : null;

    public void Tick(float deltaTime, Vector2 movementInput, bool interactPressed)
    {
        if (!IsRaidResolved)
        {
            _timeRemainingSeconds -= deltaTime;
            if (_timeRemainingSeconds <= 0f)
            {
                _timeRemainingSeconds = 0f;
                Phase = RaidPhase.Failed;
            }
        }

        MovePlayer(movementInput, deltaTime);
        UpdateInteractionState();

        if (interactPressed)
        {
            TryInteract();
        }
    }

    private void MovePlayer(Vector2 movementInput, float deltaTime)
    {
        var delta = Player.CreateMoveDelta(movementInput, deltaTime);
        if (delta.LengthSquared() <= float.Epsilon)
        {
            return;
        }

        var current = Player.Transform.Position;
        var candidateX = new Vector3(current.X + delta.X, current.Y, current.Z);
        if (!Collides(candidateX))
        {
            current = candidateX;
        }

        var candidateZ = new Vector3(current.X, current.Y, current.Z + delta.Z);
        if (!Collides(candidateZ))
        {
            current = candidateZ;
        }

        current.X = Math.Clamp(current.X, Sector.BoundsMin.X + Player.CollisionRadius, Sector.BoundsMax.X - Player.CollisionRadius);
        current.Z = Math.Clamp(current.Z, Sector.BoundsMin.Z + Player.CollisionRadius, Sector.BoundsMax.Z - Player.CollisionRadius);
        Player.SetPosition(current);
    }

    private bool Collides(Vector3 worldPosition)
    {
        var center = new Vector2(worldPosition.X, worldPosition.Z);
        foreach (var collider in _solidColliders)
        {
            if (collider.IntersectsCircle(center, Player.CollisionRadius))
            {
                return true;
            }
        }

        foreach (var passage in ActiveLockablePassages)
        {
            if (_lockablePassageColliders.TryGetValue(passage.Id, out var collider) && collider.IntersectsCircle(center, Player.CollisionRadius))
            {
                return true;
            }
        }

        return false;
    }

    private void BuildSolidColliders()
    {
        foreach (var renderable in Sector.StaticGeometry)
        {
            AddCollider(renderable.Transform, _solidColliders);
        }

        foreach (var prop in Sector.Props)
        {
            AddCollider(prop.Renderable.Transform, _solidColliders);
        }

        foreach (var passage in Sector.LockablePassages)
        {
            AddCollider(passage.Renderable.Transform, _lockablePassageColliders, passage.Id);
        }
    }

    private static void AddCollider(Transform transform, ICollection<CollisionBox2D> target)
    {
        if (transform.Scale.Y < 0.3f)
        {
            return;
        }

        var halfExtents = GetPlanarHalfExtents(transform);
        var min = new Vector2(transform.Position.X - halfExtents.X, transform.Position.Z - halfExtents.Y);
        var max = new Vector2(transform.Position.X + halfExtents.X, transform.Position.Z + halfExtents.Y);
        target.Add(new CollisionBox2D(min, max));
    }

    private static void AddCollider(Transform transform, IDictionary<string, CollisionBox2D> target, string id)
    {
        if (transform.Scale.Y < 0.3f)
        {
            return;
        }

        var halfExtents = GetPlanarHalfExtents(transform);
        var min = new Vector2(transform.Position.X - halfExtents.X, transform.Position.Z - halfExtents.Y);
        var max = new Vector2(transform.Position.X + halfExtents.X, transform.Position.Z + halfExtents.Y);
        target[id] = new CollisionBox2D(min, max);
    }

    private static Vector2 GetPlanarHalfExtents(Transform transform)
    {
        var halfX = transform.Scale.X * 0.5f;
        var halfZ = transform.Scale.Z * 0.5f;
        var quarterTurns = (int)MathF.Round(transform.Rotation.Y / (MathF.PI * 0.5f));
        var normalizedQuarterTurns = ((quarterTurns % 4) + 4) % 4;
        return normalizedQuarterTurns % 2 == 0
            ? new Vector2(halfX, halfZ)
            : new Vector2(halfZ, halfX);
    }

    private void UpdateInteractionState()
    {
        CanInteract = false;
        InteractionPrompt = string.Empty;
        ContextHint = string.Empty;

        if (Phase == RaidPhase.RestorePower && IsWithinInteractRange(PowerSwitchPoint))
        {
            CanInteract = true;
            InteractionPrompt = "E — поднять рубильник и открыть аварийную переборку";
            return;
        }

        if (Phase == RaidPhase.ReachObjective && IsWithinInteractRange(ObjectivePoint))
        {
            CanInteract = true;
            InteractionPrompt = "E — забрать архивные журналы";
            return;
        }

        if (Phase == RaidPhase.ReturnToSafeBlock && IsWithinInteractRange(ExtractionPoint))
        {
            CanInteract = true;
            InteractionPrompt = "E — закрыть герму и завершить рейд";
            return;
        }

        foreach (var passage in ActiveLockablePassages)
        {
            if (IsWithinInteractRange(passage.Renderable.Transform.Position))
            {
                ContextHint = $"{passage.Label} закрыта. Питание снимается в service nook";
                return;
            }
        }
    }

    private void TryInteract()
    {
        if (!CanInteract)
        {
            return;
        }

        switch (Phase)
        {
            case RaidPhase.RestorePower:
                _unlockedPassages.UnionWith(Sector.LockablePassages.Where(x => x.UnlockId == "service_power").Select(x => x.Id));
                Phase = RaidPhase.ReachObjective;
                break;
            case RaidPhase.ReachObjective:
                Phase = RaidPhase.ReturnToSafeBlock;
                break;
            case RaidPhase.ReturnToSafeBlock:
                Phase = RaidPhase.Extracted;
                break;
        }

        UpdateInteractionState();
    }

    private bool IsWithinInteractRange(Vector3 worldPoint)
    {
        var playerXZ = new Vector2(Player.Transform.Position.X, Player.Transform.Position.Z);
        var pointXZ = new Vector2(worldPoint.X, worldPoint.Z);
        return Vector2.DistanceSquared(playerXZ, pointXZ) <= _config.InteractRadius * _config.InteractRadius;
    }
}

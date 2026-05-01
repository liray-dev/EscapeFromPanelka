using System.Numerics;
using EFP.Entities;

namespace EFP.World;

public sealed class HostileEntity
{
    private readonly float _anchorPhase;
    private float _investigationTimer;
    private float _patrolAngle;
    private Vector3 _searchOrigin;
    private float _searchTimer;

    public HostileEntity(string id, string label, Vector3 anchorPosition, Vector4 dormantTint, Vector4 alertTint)
    {
        Id = id;
        Label = label;
        AnchorPosition = anchorPosition;
        DormantTint = dormantTint;
        AlertTint = alertTint;
        _anchorPhase = MathF.Abs(id.GetHashCode()) * 0.0137f;
        _patrolAngle = _anchorPhase;
        _searchOrigin = anchorPosition;
        Transform = new Transform
        {
            Position = anchorPosition + new Vector3(0f, 0.58f, 0f),
            Scale = new Vector3(0.78f, 1.16f, 0.78f)
        };
    }

    public string Id { get; }
    public string Label { get; }
    private Vector3 AnchorPosition { get; }
    public Transform Transform { get; }
    public float CollisionRadius { get; } = 0.34f;
    public HostileAwarenessState AwarenessState { get; private set; }

    public bool Alerted => AwarenessState is HostileAwarenessState.Suspicious or HostileAwarenessState.Hunt
        or HostileAwarenessState.Search;

    public bool CanStrike => AwarenessState == HostileAwarenessState.Hunt;
    private Vector3 LastKnownPlayerPosition { get; set; }

    public string StateLabel => AwarenessState switch
    {
        HostileAwarenessState.Idle => "Idle",
        HostileAwarenessState.Patrol => "Patrol",
        HostileAwarenessState.Suspicious => "Suspicious",
        HostileAwarenessState.Hunt => "Hunt",
        HostileAwarenessState.Search => "Search",
        _ => "Idle"
    };

    public Vector4 DormantTint { get; }
    public Vector4 AlertTint { get; }

    public Vector4 Tint => AwarenessState switch
    {
        HostileAwarenessState.Idle => DormantTint,
        HostileAwarenessState.Patrol => Vector4.Lerp(DormantTint, AlertTint, 0.20f),
        HostileAwarenessState.Suspicious => Vector4.Lerp(DormantTint, new Vector4(0.86f, 0.68f, 0.28f, 1f), 0.72f),
        HostileAwarenessState.Hunt => AlertTint,
        HostileAwarenessState.Search => Vector4.Lerp(DormantTint, AlertTint, 0.46f),
        _ => DormantTint
    };

    public void Tick(float deltaTime, Vector3 playerPosition, float playerNoiseLevel, float playerVisibilityLevel,
        RaidPressureLevel pressureLevel, bool playerInInfectedZone,
        Func<Vector3, float, bool> collides,
        Func<Vector3, Vector3, bool> hasLineOfSight)
    {
        var current = Transform.Position;
        var toPlayer = new Vector2(playerPosition.X - current.X, playerPosition.Z - current.Z);
        var distanceToPlayer = toPlayer.Length();
        var seesPlayer = CanSeePlayer(toPlayer, distanceToPlayer, playerPosition, playerVisibilityLevel, pressureLevel,
            hasLineOfSight);
        var hearsPlayer = CanHearPlayer(distanceToPlayer, playerNoiseLevel, pressureLevel, playerInInfectedZone,
            hasLineOfSight(current, playerPosition));

        if (seesPlayer)
        {
            AwarenessState = HostileAwarenessState.Hunt;
            LastKnownPlayerPosition = playerPosition;
            _searchOrigin = playerPosition;
            _searchTimer = ResolveSearchDuration(pressureLevel);
            _investigationTimer = 0f;
        }
        else
        {
            switch (AwarenessState)
            {
                case HostileAwarenessState.Hunt:
                    if (hearsPlayer)
                    {
                        LastKnownPlayerPosition = playerPosition;
                        _searchOrigin = playerPosition;
                        _searchTimer = ResolveSearchDuration(pressureLevel);
                    }
                    else
                    {
                        _searchTimer -= deltaTime;
                        if (_searchTimer <= 0f)
                        {
                            AwarenessState = HostileAwarenessState.Search;
                            _searchTimer = ResolveSearchDuration(pressureLevel) * 0.8f;
                        }
                    }

                    break;
                case HostileAwarenessState.Suspicious:
                    if (hearsPlayer)
                    {
                        LastKnownPlayerPosition = playerPosition;
                        _searchOrigin = playerPosition;
                        _investigationTimer = ResolveSuspicionDuration(pressureLevel);
                    }
                    else
                    {
                        _investigationTimer -= deltaTime;
                        if (_investigationTimer <= 0f)
                        {
                            AwarenessState = HostileAwarenessState.Search;
                            _searchTimer = ResolveSearchDuration(pressureLevel) * 0.65f;
                        }
                    }

                    break;
                case HostileAwarenessState.Search:
                    if (hearsPlayer)
                    {
                        AwarenessState = HostileAwarenessState.Suspicious;
                        LastKnownPlayerPosition = playerPosition;
                        _searchOrigin = playerPosition;
                        _investigationTimer = ResolveSuspicionDuration(pressureLevel);
                    }
                    else
                    {
                        _searchTimer -= deltaTime;
                        if (_searchTimer <= 0f)
                            AwarenessState = pressureLevel == RaidPressureLevel.Stable
                                ? HostileAwarenessState.Idle
                                : HostileAwarenessState.Patrol;
                    }

                    break;
                case HostileAwarenessState.Idle:
                case HostileAwarenessState.Patrol:
                default:
                    if (hearsPlayer)
                    {
                        AwarenessState = HostileAwarenessState.Suspicious;
                        LastKnownPlayerPosition = playerPosition;
                        _searchOrigin = playerPosition;
                        _investigationTimer = ResolveSuspicionDuration(pressureLevel);
                    }
                    else
                    {
                        AwarenessState = pressureLevel == RaidPressureLevel.Stable
                            ? HostileAwarenessState.Idle
                            : HostileAwarenessState.Patrol;
                    }

                    break;
            }
        }

        var planarDirection = ResolveDirection(current, pressureLevel, playerPosition, deltaTime);
        var speed = ResolveSpeed(pressureLevel);
        var delta = new Vector3(planarDirection.X, 0f, planarDirection.Y) * (speed * deltaTime);

        if (delta.LengthSquared() > float.Epsilon)
        {
            var next = current;
            var candidateX = new Vector3(current.X + delta.X, current.Y, current.Z);
            if (!collides(candidateX, CollisionRadius)) next = candidateX;

            var candidateZ = new Vector3(next.X, current.Y, current.Z + delta.Z);
            if (!collides(candidateZ, CollisionRadius)) next = candidateZ;

            Transform.Position = new Vector3(next.X, Transform.Position.Y, next.Z);
        }

        if (planarDirection.LengthSquared() > 0.001f)
            Transform.Rotation = new Vector3(0f, MathF.Atan2(planarDirection.X, planarDirection.Y), 0f);
    }

    private bool CanSeePlayer(Vector2 toPlayer, float distanceToPlayer, Vector3 playerPosition, float playerVisibility,
        RaidPressureLevel pressureLevel, Func<Vector3, Vector3, bool> hasLineOfSight)
    {
        if (distanceToPlayer <= 0.01f) return true;

        var baseVisionRadius = pressureLevel switch
        {
            RaidPressureLevel.Stable => 2.2f,
            RaidPressureLevel.Pressure => 4.3f,
            RaidPressureLevel.Critical => 6.1f,
            _ => 2.2f
        };

        var visibilityFactor = 0.62f + (1.35f - 0.62f) * Math.Clamp(playerVisibility, 0f, 1f);
        var viewRadius = baseVisionRadius * visibilityFactor;
        if (distanceToPlayer > viewRadius) return false;
        if (!hasLineOfSight(Transform.Position, playerPosition)) return false;

        var forward = new Vector2(MathF.Sin(Transform.Rotation.Y), MathF.Cos(Transform.Rotation.Y));
        if (forward.LengthSquared() <= 0.0001f) forward = Vector2.UnitY;

        var directionToPlayer = Vector2.Normalize(toPlayer);
        var halfFovDegrees = AwarenessState switch
        {
            HostileAwarenessState.Hunt => 82f,
            HostileAwarenessState.Search => 64f,
            _ => pressureLevel switch
            {
                RaidPressureLevel.Stable => 44f,
                RaidPressureLevel.Pressure => 56f,
                RaidPressureLevel.Critical => 68f,
                _ => 44f
            }
        };

        var minDot = MathF.Cos(MathF.PI / 180f * halfFovDegrees);
        return Vector2.Dot(forward, directionToPlayer) >= minDot;
    }

    private bool CanHearPlayer(float distanceToPlayer, float playerNoiseLevel, RaidPressureLevel pressureLevel,
        bool playerInInfectedZone, bool hasClearPath)
    {
        if (playerNoiseLevel <= 0.01f) return false;

        var baseHearingRadius = pressureLevel switch
        {
            RaidPressureLevel.Stable => 2.1f,
            RaidPressureLevel.Pressure => 3.2f,
            RaidPressureLevel.Critical => 4.4f,
            _ => 2.1f
        };

        var noiseRadius = baseHearingRadius * (0.45f + playerNoiseLevel * 1.45f);
        if (!hasClearPath) noiseRadius *= 0.62f;
        if (playerInInfectedZone) noiseRadius += 0.8f;

        return distanceToPlayer <= noiseRadius;
    }

    private Vector2 ResolveDirection(Vector3 current, RaidPressureLevel pressureLevel, Vector3 playerPosition,
        float deltaTime)
    {
        return AwarenessState switch
        {
            HostileAwarenessState.Hunt => ResolveTargetDirection(current, playerPosition),
            HostileAwarenessState.Suspicious => ResolveTargetDirection(current, LastKnownPlayerPosition),
            HostileAwarenessState.Search => ResolveSearchDirection(current, pressureLevel, deltaTime),
            HostileAwarenessState.Patrol => ResolvePatrolDirection(current, pressureLevel, deltaTime),
            HostileAwarenessState.Idle => ResolveIdleDirection(current),
            _ => Vector2.Zero
        };
    }

    private Vector2 ResolveIdleDirection(Vector3 current)
    {
        var toAnchor = new Vector2(AnchorPosition.X - current.X, AnchorPosition.Z - current.Z);
        return toAnchor.LengthSquared() > 0.35f ? Vector2.Normalize(toAnchor) : Vector2.Zero;
    }

    private Vector2 ResolvePatrolDirection(Vector3 current, RaidPressureLevel pressureLevel, float deltaTime)
    {
        _patrolAngle += ResolveOrbitSpeed(pressureLevel) * deltaTime;
        var patrolRadius = pressureLevel == RaidPressureLevel.Critical ? 1.05f : 0.72f;
        var patrolTarget = AnchorPosition + new Vector3(
            MathF.Cos(_patrolAngle + _anchorPhase) * patrolRadius,
            0f,
            MathF.Sin(_patrolAngle + _anchorPhase) * patrolRadius);
        return ResolveTargetDirection(current, patrolTarget);
    }

    private Vector2 ResolveSearchDirection(Vector3 current, RaidPressureLevel pressureLevel, float deltaTime)
    {
        _patrolAngle += ResolveOrbitSpeed(pressureLevel) * 1.2f * deltaTime;
        var searchRadius = pressureLevel == RaidPressureLevel.Critical ? 1.30f : 0.88f;
        var searchTarget = _searchOrigin + new Vector3(
            MathF.Cos(_patrolAngle + _anchorPhase) * searchRadius,
            0f,
            MathF.Sin(_patrolAngle + _anchorPhase) * searchRadius);
        return ResolveTargetDirection(current, searchTarget);
    }

    private static Vector2 ResolveTargetDirection(Vector3 current, Vector3 target)
    {
        var toTarget = new Vector2(target.X - current.X, target.Z - current.Z);
        return toTarget.LengthSquared() > 0.0025f ? Vector2.Normalize(toTarget) : Vector2.Zero;
    }

    private float ResolveSpeed(RaidPressureLevel pressureLevel)
    {
        return AwarenessState switch
        {
            HostileAwarenessState.Idle => 0.18f,
            HostileAwarenessState.Patrol => pressureLevel == RaidPressureLevel.Critical ? 1.26f : 0.88f,
            HostileAwarenessState.Suspicious => pressureLevel == RaidPressureLevel.Critical ? 1.65f : 1.28f,
            HostileAwarenessState.Hunt => pressureLevel switch
            {
                RaidPressureLevel.Stable => 1.90f,
                RaidPressureLevel.Pressure => 2.55f,
                RaidPressureLevel.Critical => 3.22f,
                _ => 1.90f
            },
            HostileAwarenessState.Search => pressureLevel == RaidPressureLevel.Critical ? 1.42f : 1.05f,
            _ => 0.18f
        };
    }

    private static float ResolveOrbitSpeed(RaidPressureLevel pressureLevel)
    {
        return pressureLevel switch
        {
            RaidPressureLevel.Stable => 1.00f,
            RaidPressureLevel.Pressure => 1.55f,
            RaidPressureLevel.Critical => 2.10f,
            _ => 1.00f
        };
    }

    private static float ResolveSearchDuration(RaidPressureLevel pressureLevel)
    {
        return pressureLevel switch
        {
            RaidPressureLevel.Stable => 1.8f,
            RaidPressureLevel.Pressure => 2.7f,
            RaidPressureLevel.Critical => 3.8f,
            _ => 1.8f
        };
    }

    private static float ResolveSuspicionDuration(RaidPressureLevel pressureLevel)
    {
        return pressureLevel switch
        {
            RaidPressureLevel.Stable => 1.4f,
            RaidPressureLevel.Pressure => 2.0f,
            RaidPressureLevel.Critical => 2.8f,
            _ => 1.4f
        };
    }
}
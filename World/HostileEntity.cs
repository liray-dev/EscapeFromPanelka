using System.Numerics;
using EFP.Entities;

namespace EFP.World;

public sealed class HostileEntity
{
    private readonly float _anchorPhase;
    private float _patrolAngle;

    public HostileEntity(string id, string label, Vector3 anchorPosition, Vector4 dormantTint, Vector4 alertTint)
    {
        Id = id;
        Label = label;
        AnchorPosition = anchorPosition;
        DormantTint = dormantTint;
        AlertTint = alertTint;
        _anchorPhase = MathF.Abs(id.GetHashCode()) * 0.0137f;
        _patrolAngle = _anchorPhase;
        Transform = new Transform
        {
            Position = anchorPosition + new Vector3(0f, 0.58f, 0f),
            Scale = new Vector3(0.78f, 1.16f, 0.78f)
        };
    }

    public string Id { get; }
    public string Label { get; }
    public Vector3 AnchorPosition { get; }
    public Transform Transform { get; }
    public float CollisionRadius { get; } = 0.34f;
    public bool Alerted { get; private set; }
    public string StateLabel { get; private set; } = "Dormant";
    public Vector4 DormantTint { get; }
    public Vector4 AlertTint { get; }

    public Vector4 Tint => Alerted ? AlertTint : DormantTint;

    public void Tick(float deltaTime, Vector3 playerPosition, RaidPressureLevel pressureLevel,
        bool playerInInfectedZone,
        Func<Vector3, float, bool> collides)
    {
        var current = Transform.Position;
        var toPlayer = new Vector2(playerPosition.X - current.X, playerPosition.Z - current.Z);
        var distanceToPlayer = toPlayer.Length();
        var detectionRadius = pressureLevel switch
        {
            RaidPressureLevel.Stable => 1.8f,
            RaidPressureLevel.Pressure => 4.8f,
            RaidPressureLevel.Critical => 7.4f,
            _ => 1.8f
        };

        if (playerInInfectedZone) detectionRadius += 1.8f;

        Alerted = pressureLevel != RaidPressureLevel.Stable && distanceToPlayer <= detectionRadius;

        var speed = pressureLevel switch
        {
            RaidPressureLevel.Stable => 0.55f,
            RaidPressureLevel.Pressure => Alerted ? 2.05f : 0.92f,
            RaidPressureLevel.Critical => Alerted ? 2.85f : 1.30f,
            _ => 0.55f
        };

        Vector2 planarDirection;
        if (Alerted && distanceToPlayer > 0.01f)
        {
            planarDirection = Vector2.Normalize(toPlayer);
            StateLabel = "Hunt";
        }
        else
        {
            _patrolAngle += deltaTime * (pressureLevel == RaidPressureLevel.Critical ? 1.75f : 0.95f);
            var patrolRadius = pressureLevel == RaidPressureLevel.Stable ? 0.35f : 0.85f;
            var patrolTarget = AnchorPosition + new Vector3(
                MathF.Cos(_patrolAngle + _anchorPhase) * patrolRadius,
                0f,
                MathF.Sin(_patrolAngle + _anchorPhase) * patrolRadius);
            var toPatrol = new Vector2(patrolTarget.X - current.X, patrolTarget.Z - current.Z);
            planarDirection = toPatrol.LengthSquared() > 0.0025f ? Vector2.Normalize(toPatrol) : Vector2.Zero;
            StateLabel = pressureLevel == RaidPressureLevel.Stable ? "Dormant" : "Patrol";
        }

        var delta = new Vector3(planarDirection.X, 0f, planarDirection.Y) * (speed * deltaTime);
        if (delta.LengthSquared() <= float.Epsilon) return;

        var next = current;
        var candidateX = new Vector3(current.X + delta.X, current.Y, current.Z);
        if (!collides(candidateX, CollisionRadius)) next = candidateX;

        var candidateZ = new Vector3(next.X, current.Y, current.Z + delta.Z);
        if (!collides(candidateZ, CollisionRadius)) next = candidateZ;

        Transform.Position = new Vector3(next.X, Transform.Position.Y, next.Z);

        if (planarDirection.LengthSquared() > 0.001f)
            Transform.Rotation = new Vector3(0f, MathF.Atan2(planarDirection.X, planarDirection.Y), 0f);
    }
}
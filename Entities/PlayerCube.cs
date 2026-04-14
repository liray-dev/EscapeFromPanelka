using System.Numerics;

namespace EFP.Entities;

public sealed class PlayerCube(float moveSpeed, float collisionRadius, Vector3? spawnPosition = null)
{
    public Transform Transform { get; } = new()
    {
        Position = spawnPosition ?? new Vector3(0f, 0.5f, 0f),
        Scale = Vector3.One
    };

    public float CollisionRadius { get; } = collisionRadius;

    public float YawRadians => Transform.Rotation.Y;
    public float YawDegrees => Transform.Rotation.Y * 180f / MathF.PI;

    public Vector3 CreateMoveDelta(Vector2 moveInput, float deltaTime)
    {
        var direction = new Vector3(moveInput.X, 0f, moveInput.Y);
        if (direction.LengthSquared() > 1f) direction = Vector3.Normalize(direction);

        return direction * (moveSpeed * deltaTime);
    }

    public void Move(Vector2 moveInput, float deltaTime)
    {
        SetPosition(Transform.Position + CreateMoveDelta(moveInput, deltaTime));
    }

    public void SetPosition(Vector3 position)
    {
        Transform.Position = position with { Y = 0.5f };
    }

    public void RotateYaw(float deltaYawRadians)
    {
        Transform.Rotation = Transform.Rotation with { Y = NormalizeAngle(Transform.Rotation.Y + deltaYawRadians) };
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.Tau;

        while (angle < -MathF.PI) angle += MathF.Tau;

        return angle;
    }
}
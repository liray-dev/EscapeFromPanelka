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
        var localDirection = new Vector3(moveInput.X, 0f, moveInput.Y);
        if (localDirection.LengthSquared() > 1f) localDirection = Vector3.Normalize(localDirection);

        var rotatedDirection = Vector3.Transform(localDirection,
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, Transform.Rotation.Y));

        return rotatedDirection * (moveSpeed * deltaTime);
    }

    public void Move(Vector2 moveInput, float deltaTime)
    {
        SetPosition(Transform.Position + CreateMoveDelta(moveInput, deltaTime));
    }

    public void SetPosition(Vector3 position)
    {
        Transform.Position = new Vector3(position.X, 0.5f, position.Z);
    }

    public void RotateYaw(float deltaYawRadians)
    {
        Transform.Rotation = new Vector3(Transform.Rotation.X, NormalizeAngle(Transform.Rotation.Y + deltaYawRadians),
            Transform.Rotation.Z);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.Tau;
        while (angle < -MathF.PI) angle += MathF.Tau;
        return angle;
    }
}
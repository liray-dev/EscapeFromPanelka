using System.Numerics;

namespace EFP.Entities;

public sealed class PlayerCube(float moveSpeed)
{
    public Transform Transform { get; } = new()
    {
        Position = new Vector3(0f, 0.5f, 0f),
        Scale = Vector3.One
    };

    public float YawDegrees => Transform.Rotation.Y * 180f / MathF.PI;

    public void Move(Vector2 moveInput, float deltaTime)
    {
        var direction = new Vector3(moveInput.X, 0f, moveInput.Y);
        if (direction.LengthSquared() > 1f)
        {
            direction = Vector3.Normalize(direction);
        }

        Transform.Position += direction * (moveSpeed * deltaTime);
        Transform.Position = Transform.Position with { Y = 0.5f };
    }

    public void RotateYaw(float deltaYaw)
    {
        Transform.Rotation = Transform.Rotation with { Y = NormalizeAngle(Transform.Rotation.Y + deltaYaw) };
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI)
        {
            angle -= MathF.Tau;
        }

        while (angle < -MathF.PI)
        {
            angle += MathF.Tau;
        }

        return angle;
    }
}
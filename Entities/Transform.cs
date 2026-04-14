using System.Numerics;

namespace EFP.Entities;

public sealed class Transform
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 Scale { get; set; } = Vector3.One;

    public Matrix4x4 CreateModelMatrix()
    {
        return Matrix4x4.CreateScale(Scale)
               * Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z)
               * Matrix4x4.CreateTranslation(Position);
    }
}
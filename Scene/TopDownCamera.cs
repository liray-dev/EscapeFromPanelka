using System.Numerics;
using EFP.App;

namespace EFP.Scene;

public sealed class TopDownCamera
{
    private readonly CameraConfig _config;
    private float _aspectRatio = 16f / 9f;

    public TopDownCamera(CameraConfig config)
    {
        _config = config;
        RebuildMatrices();
    }

    private Vector3 Position { get; set; }
    private Vector3 Target { get; set; }
    public Matrix4x4 View { get; private set; }
    public Matrix4x4 Projection { get; private set; }

    public void Resize(int width, int height)
    {
        _aspectRatio = height <= 0 ? _aspectRatio : width / (float)height;
        RebuildMatrices();
    }

    public void Follow(Vector3 target)
    {
        Target = target;
        RebuildMatrices();
    }

    private void RebuildMatrices()
    {
        Position = Target + new Vector3(0f, _config.Height, _config.Distance);
        View = Matrix4x4.CreateLookAt(Position, Target, Vector3.UnitY);
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(
            DegreesToRadians(_config.FovDegrees),
            _aspectRatio,
            0.1f,
            200f);
    }

    private static float DegreesToRadians(float degrees)
    {
        return degrees * MathF.PI / 180f;
    }
}
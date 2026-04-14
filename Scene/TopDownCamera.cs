using System.Numerics;
using EFP.App;
using EFP.Utilities;

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
        var yaw = MathHelperEx.DegreesToRadians(_config.YawDegrees);
        var pitch = MathHelperEx.DegreesToRadians(_config.PitchDegrees);

        var flatForward = new Vector3(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        var horizontalDistance = _config.Distance * MathF.Cos(pitch);
        var verticalDistance = _config.Height + _config.Distance * MathF.Sin(pitch);

        Position = Target - flatForward * horizontalDistance + Vector3.UnitY * verticalDistance;
        View = Matrix4x4.CreateLookAt(Position, Target, Vector3.UnitY);
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathHelperEx.DegreesToRadians(_config.FovDegrees),
            _aspectRatio,
            0.1f,
            200f);
    }
}
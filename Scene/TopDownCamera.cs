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
        Target = target + new Vector3(0f, 0.8f, 0f);
        RebuildMatrices();
    }

    private void RebuildMatrices()
    {
        var yaw = MathHelperEx.DegreesToRadians(_config.YawDegrees);
        var pitch = MathHelperEx.DegreesToRadians(Math.Clamp(_config.PitchDegrees, 15f, 80f));

        var horizontalRadius = _config.Distance * MathF.Cos(pitch);
        var verticalOffset = _config.Height + (_config.Distance * MathF.Sin(pitch));

        var offset = new Vector3(
            MathF.Cos(yaw) * horizontalRadius,
            verticalOffset,
            MathF.Sin(yaw) * horizontalRadius);

        Position = Target + offset;
        View = Matrix4x4.CreateLookAt(Position, Target, Vector3.UnitY);
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathHelperEx.DegreesToRadians(_config.FovDegrees),
            _aspectRatio,
            0.05f,
            250f);
    }
}

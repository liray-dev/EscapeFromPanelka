using System.Numerics;
using EFP.App;
using EFP.Utilities;

namespace EFP.View.Camera;

public sealed class TopDownCamera
{
    private readonly CameraConfig _config;
    private float _aspectRatio = 16f / 9f;
    private float _distance;
    private float _height;
    private bool _initialized;
    private float _pitchDegrees;
    private Vector3 _target;
    private float _yawDegrees;

    public TopDownCamera(CameraConfig config)
    {
        _config = config;
        _yawDegrees = config.YawDegrees;
        _pitchDegrees = config.PitchDegrees;
        _distance = config.Distance;
        _height = config.Height;
        RebuildMatrices();
    }

    public Vector3 Position { get; private set; }
    public Matrix4x4 View { get; private set; }
    public Matrix4x4 Projection { get; private set; }

    public void Resize(int width, int height)
    {
        _aspectRatio = height <= 0 ? _aspectRatio : width / (float)height;
        RebuildMatrices();
    }

    public void ApplyDebug(DebugSettings settings)
    {
        _yawDegrees = settings.CameraYawDegrees;
        _pitchDegrees = settings.CameraPitchDegrees;
        _distance = settings.CameraDistance;
        _height = settings.CameraHeight;
        RebuildMatrices();
    }

    public void Follow(Vector3 target, float smoothingFactor, float deltaTime)
    {
        var desiredTarget = target + new Vector3(0f, 0.8f, 0f);

        if (!_initialized)
        {
            _target = desiredTarget;
            _initialized = true;
        }
        else if (smoothingFactor >= 0.999f)
        {
            _target = desiredTarget;
        }
        else
        {
            var alpha = 1f - MathF.Pow(1f - Math.Clamp(smoothingFactor, 0.01f, 0.999f), deltaTime * 60f);
            _target = Vector3.Lerp(_target, desiredTarget, alpha);
        }

        RebuildMatrices();
    }

    public void SnapTo(Vector3 target)
    {
        _target = target + new Vector3(0f, 0.8f, 0f);
        _initialized = true;
        RebuildMatrices();
    }

    private void RebuildMatrices()
    {
        var yaw = MathHelperEx.DegreesToRadians(_yawDegrees);
        var pitch = MathHelperEx.DegreesToRadians(Math.Clamp(_pitchDegrees, 15f, 82f));
        var distance = MathF.Max(2f, _distance);
        var height = MathF.Max(0f, _height);

        var horizontalRadius = distance * MathF.Cos(pitch);
        var verticalOffset = height + distance * MathF.Sin(pitch);

        var offset = new Vector3(
            MathF.Cos(yaw) * horizontalRadius,
            verticalOffset,
            MathF.Sin(yaw) * horizontalRadius);

        Position = _target + offset;
        View = Matrix4x4.CreateLookAt(Position, _target, Vector3.UnitY);
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathHelperEx.DegreesToRadians(_config.FovDegrees),
            _aspectRatio,
            0.05f,
            250f);
    }
}
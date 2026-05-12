using System.Numerics;
using EFP.Model.Raid;
using EFP.View.Camera;
using RaidModel = EFP.Model.Raid.Raid;

namespace EFP.View.Rendering;

public sealed class MeleeTracerView : IDisposable
{
    private const float SwingDuration = 0.14f;
    private const float TrailDuration = 0.18f;
    private const int MaxSamples = 28;
    private const float SwingHalfAngleRadians = 1.25f;
    private const float BaseWidth = 0.55f;
    private const float HeightOffset = 0.95f;
    private const float OriginPushForward = 0.25f;
    private const float WindUpFraction = 0.16f;

    private readonly RaidModel _raid;
    private readonly Random _rng = new();
    private readonly List<Swing> _swings = [];
    private readonly List<SwingSample> _sampleBuffer = new(MaxSamples);

    public MeleeTracerView(RaidModel raid)
    {
        _raid = raid;
        _raid.AttackPerformed += OnAttack;
    }

    public void Dispose()
    {
        _raid.AttackPerformed -= OnAttack;
    }

    public void Update(float deltaTime)
    {
        for (var i = _swings.Count - 1; i >= 0; i--)
        {
            _swings[i].Age += deltaTime;
            if (_swings[i].Age >= SwingDuration + TrailDuration) _swings.RemoveAt(i);
        }
    }

    public void Render(SceneRenderer renderer, TopDownCamera camera)
    {
        foreach (var swing in _swings)
        {
            var forwardXZ = new Vector3(MathF.Sin(swing.ForwardAngle), 0f, MathF.Cos(swing.ForwardAngle));
            var origin = swing.Origin + new Vector3(0f, HeightOffset, 0f) + forwardXZ * OriginPushForward;

            var swingProgress = MathF.Min(1f, swing.Age / SwingDuration);
            var fadeT = MathF.Max(0f, (swing.Age - SwingDuration) / TrailDuration);
            var lifeFade = 1f - fadeT;
            if (lifeFade <= 0.001f) continue;

            var sampleCount = Math.Max(2, (int)MathF.Round(MaxSamples * MathF.Max(0.12f, swingProgress)));
            _sampleBuffer.Clear();

            var headEase = ApplyWindUp(swingProgress);
            var coreColor = swing.Critical
                ? new Vector3(2.0f, 1.55f, 0.55f)
                : new Vector3(0.55f, 0.85f, 1.55f);

            for (var i = 0; i < sampleCount; i++)
            {
                var localT = (float)i / (sampleCount - 1);
                var arcT = ApplyWindUp(localT * swingProgress);
                var angle = swing.ForwardAngle
                            + (arcT * 2f - 1f) * SwingHalfAngleRadians * swing.Sign
                            + swing.RandomOffsetRadians;

                var direction = new Vector3(MathF.Sin(angle), 0f, MathF.Cos(angle));
                var reach = swing.Range * (0.82f + 0.18f * MathF.Sin(arcT * MathF.PI));
                var position = origin + direction * reach;

                var headProximity = MathF.Pow(localT, 1.6f);
                var alphaCurve = (0.18f + headProximity) * lifeFade;
                var width = BaseWidth * (0.55f + 0.55f * MathF.Sin(arcT * MathF.PI));

                _sampleBuffer.Add(new SwingSample(position, coreColor, alphaCurve, width));
            }

            renderer.DrawSwingRibbon(_sampleBuffer, camera);
        }
    }

    private static float ApplyWindUp(float linearProgress)
    {
        if (linearProgress <= 0f) return -0.05f;
        if (linearProgress >= WindUpFraction)
        {
            var t = (linearProgress - WindUpFraction) / (1f - WindUpFraction);
            return EaseOutCubic(t);
        }

        var k = linearProgress / WindUpFraction;
        return -0.05f + 0.05f * k * k;
    }

    private static float EaseOutCubic(float t)
    {
        var inverted = 1f - t;
        return 1f - inverted * inverted * inverted;
    }

    private void OnAttack(object? sender, AttackPerformed e)
    {
        var direction = e.Direction;
        if (direction.LengthSquared() <= 0.0001f) direction = -Vector3.UnitZ;
        var forwardAngle = MathF.Atan2(direction.X, direction.Z);
        var sign = _rng.Next(2) == 0 ? -1f : 1f;
        var randomOffset = ((float)_rng.NextDouble() - 0.5f) * 0.18f;

        _swings.Add(new Swing
        {
            Origin = e.Origin,
            ForwardAngle = forwardAngle,
            Sign = sign,
            RandomOffsetRadians = randomOffset,
            Range = e.Range,
            Critical = e.Attack.Critical,
            Age = 0f
        });
    }

    private sealed class Swing
    {
        public Vector3 Origin;
        public float ForwardAngle;
        public float Sign;
        public float RandomOffsetRadians;
        public float Range;
        public bool Critical;
        public float Age;
    }
}

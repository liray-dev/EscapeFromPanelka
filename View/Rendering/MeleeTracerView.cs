using System.Numerics;
using EFP.Model.Raid;
using EFP.View.Camera;
using RaidModel = EFP.Model.Raid.Raid;

namespace EFP.View.Rendering;

public sealed class MeleeTracerView : IDisposable
{
    private const float Lifetime = 0.22f;
    private const float Width = 0.06f;
    private const float HeightOffset = 0.95f;

    private readonly List<Tracer> _tracers = [];
    private readonly RaidModel _raid;

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
        for (var i = _tracers.Count - 1; i >= 0; i--)
        {
            var tracer = _tracers[i];
            tracer.Age += deltaTime;
            if (tracer.Age >= Lifetime)
            {
                _tracers.RemoveAt(i);
                continue;
            }

            _tracers[i] = tracer;
        }
    }

    public void Render(SceneRenderer renderer, TopDownCamera camera)
    {
        foreach (var tracer in _tracers)
        {
            var progress = tracer.Age / Lifetime;
            var alpha = MathF.Max(0f, 1f - progress);
            var color = tracer.Critical
                ? new Vector4(1.0f, 0.86f, 0.38f, alpha * 0.95f)
                : new Vector4(0.94f, 0.96f, 1.00f, alpha * 0.85f);

            var from = new Vector3(tracer.Origin.X, HeightOffset, tracer.Origin.Z);
            var to = from + tracer.Direction * tracer.Range;
            renderer.DrawTracer(from, to, Width, color, camera);
        }
    }

    private void OnAttack(object? sender, AttackPerformed e)
    {
        _tracers.Add(new Tracer
        {
            Origin = e.Origin,
            Direction = e.Direction,
            Range = e.Range,
            Critical = e.Attack.Critical,
            Age = 0f
        });
    }

    private struct Tracer
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float Range;
        public bool Critical;
        public float Age;
    }
}

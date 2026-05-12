using System.Numerics;
using EFP.View.Resources;
using Silk.NET.OpenGL;

namespace EFP.View.Rendering.Models;

public sealed class ModelRegistry : IDisposable
{
    private readonly Dictionary<string, ModelDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly GL _gl;
    private readonly Dictionary<string, GpuModel> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missing = new(StringComparer.OrdinalIgnoreCase);
    private readonly GameResources _resources;

    public ModelRegistry(GL gl, GameResources resources)
    {
        _gl = gl;
        _resources = resources;
    }

    public IReadOnlyCollection<string> RegisteredIds => _definitions.Keys;

    public void Dispose()
    {
        foreach (var gpu in _loaded.Values) gpu.Mesh.Dispose();
        _loaded.Clear();
    }

    public void RegisterDefaults()
    {
        foreach (var definition in ModelCatalog.Defaults)
        {
            if (string.IsNullOrWhiteSpace(definition.Id)) continue;
            _definitions[definition.Id] = definition;
        }
    }

    public bool HasDefinition(string id)
    {
        return !string.IsNullOrEmpty(id) && _definitions.ContainsKey(id);
    }

    public ModelDefinition? GetDefinition(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _definitions.GetValueOrDefault(id);
    }

    public GpuModel? Resolve(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_missing.Contains(id)) return null;
        if (_loaded.TryGetValue(id, out var existing)) return existing;
        if (!_definitions.TryGetValue(id, out var definition))
        {
            _missing.Add(id);
            return null;
        }

        var absolutePath = _resources.GetModelPath(definition.Path);
        if (!File.Exists(absolutePath))
        {
            Console.Error.WriteLine($"[ModelRegistry] Missing file for '{id}': {absolutePath}");
            _missing.Add(id);
            return null;
        }

        try
        {
            var data = ModelLoader.Load(absolutePath, definition.FlipUv);
            var mesh = new Mesh(_gl, data.Vertices, data.Indices);
            var gpu = new GpuModel(definition, mesh);
            _loaded[id] = gpu;
            return gpu;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ModelRegistry] Failed to load '{id}': {ex.Message}");
            _missing.Add(id);
            return null;
        }
    }

    public sealed class GpuModel
    {
        public GpuModel(ModelDefinition definition, Mesh mesh)
        {
            Definition = definition;
            Mesh = mesh;
            ScaleVector = new Vector3(
                definition.Scale.Length > 0 ? definition.Scale[0] : 1f,
                definition.Scale.Length > 1 ? definition.Scale[1] : 1f,
                definition.Scale.Length > 2 ? definition.Scale[2] : 1f);
            Offset = new Vector3(
                definition.OffsetMeters.Length > 0 ? definition.OffsetMeters[0] : 0f,
                definition.OffsetMeters.Length > 1 ? definition.OffsetMeters[1] : 0f,
                definition.OffsetMeters.Length > 2 ? definition.OffsetMeters[2] : 0f);
            YawOffsetRadians = MathF.PI / 180f * definition.YawOffsetDegrees;
            Tint = new Vector4(
                definition.Tint.Length > 0 ? definition.Tint[0] : 1f,
                definition.Tint.Length > 1 ? definition.Tint[1] : 1f,
                definition.Tint.Length > 2 ? definition.Tint[2] : 1f,
                definition.Tint.Length > 3 ? definition.Tint[3] : 1f);
        }

        public ModelDefinition Definition { get; }
        public Mesh Mesh { get; }
        public Vector3 ScaleVector { get; }
        public Vector3 Offset { get; }
        public float YawOffsetRadians { get; }
        public Vector4 Tint { get; }
    }
}
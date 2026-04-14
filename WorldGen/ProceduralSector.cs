using System.Numerics;
using EFP.World;

namespace EFP.WorldGen;

public sealed class ProceduralSector
{
    public int Seed { get; set; }
    public List<PlacedModule> Modules { get; set; } = [];
    public List<WorldRenderable> StaticGeometry { get; set; } = [];
    public List<WorldRenderable> FeatureGeometry { get; set; } = [];
    public List<WorldRenderable> CriticalMutationGeometry { get; set; } = [];
    public List<PropInstance> Props { get; set; } = [];
    public List<LockablePassage> LockablePassages { get; set; } = [];
    public Vector3 PlayerSpawn { get; set; }
    public Vector3 SafeBlockCenter { get; set; }
    public Vector3 ExtractionConsolePoint { get; set; }
    public Vector3 PowerSwitchPoint { get; set; }
    public Vector3 ObjectivePoint { get; set; }
    public Vector3 BoundsMin { get; set; }
    public Vector3 BoundsMax { get; set; }
}
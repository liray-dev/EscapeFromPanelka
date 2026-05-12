using System.Numerics;
using EFP.Model.Raid;
using EFP.Model.Raid.Props;

namespace EFP.Model.WorldGen;

public sealed class ProceduralSector
{
    public int Seed { get; init; }
    public float RoomSizeMultiplier { get; init; } = 1f;
    public List<PlacedModule> Modules { get; } = [];
    public List<WorldRenderable> StaticGeometry { get; } = [];
    public List<WorldRenderable> FeatureGeometry { get; } = [];
    public List<WorldRenderable> CriticalMutationGeometry { get; } = [];
    public List<PropInstance> Props { get; } = [];
    public List<LockablePassage> LockablePassages { get; } = [];
    public List<WorldLight> Lights { get; } = [];
    public List<InfectedZone> InfectedZones { get; } = [];
    public List<Hostile> Hostiles { get; } = [];
    public List<LootPickup> Loot { get; } = [];
    public List<Container> Containers { get; } = [];
    public Vector3 PlayerSpawn { get; set; }
    public Vector3 SafeBlockCenter { get; set; }
    public Vector3 ExtractionConsolePoint { get; set; }
    public Vector3 PowerSwitchPoint { get; set; }
    public Vector3 ObjectivePoint { get; set; }
    public Vector3 BoundsMin { get; set; }
    public Vector3 BoundsMax { get; set; }
}
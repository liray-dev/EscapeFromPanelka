using EFP.Model.Catalog;

namespace EFP.Model.WorldGen;

public sealed class ModuleLibrary
{
    private readonly Dictionary<string, ModuleDefinition> _modulesById;
    private readonly Dictionary<string, PropDefinition> _propsById;
    private readonly Dictionary<string, List<PropDefinition>> _propsBySlotType;

    public ModuleLibrary(float roomSizeMultiplier = 1f)
    {
        RoomSizeMultiplier = Math.Clamp(roomSizeMultiplier, 0.75f, 1.80f);

        var scaledModules = ModuleCatalog.Modules
            .Select(x => CloneModule(x, RoomSizeMultiplier))
            .ToList();

        var props = ModuleCatalog.Props
            .Select(CloneProp)
            .ToList();

        _modulesById = scaledModules.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
        _propsById = props.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
        _propsBySlotType = props
            .GroupBy(x => x.SlotType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    public float RoomSizeMultiplier { get; }

    public IReadOnlyList<ModuleDefinition> AllModules => _modulesById.Values.ToList();

    public ModuleDefinition GetModule(string id)
    {
        return _modulesById.TryGetValue(id, out var module)
            ? module
            : throw new KeyNotFoundException($"Unknown module id: {id}");
    }

    public PropDefinition GetProp(string id)
    {
        return _propsById.TryGetValue(id, out var prop)
            ? prop
            : throw new KeyNotFoundException($"Unknown prop id: {id}");
    }

    public IReadOnlyList<PropDefinition> GetPropsForSlot(string slotType)
    {
        return _propsBySlotType.TryGetValue(slotType, out var props) ? props : [];
    }

    private static ModuleDefinition CloneModule(ModuleDefinition source, float roomSizeMultiplier)
    {
        var multiplier = Math.Clamp(roomSizeMultiplier, 0.75f, 1.80f);

        return new ModuleDefinition
        {
            Id = source.Id,
            Archetype = source.Archetype,
            Width = source.Width * multiplier,
            Length = source.Length * multiplier,
            FloorHeight = source.FloorHeight,
            WallHeight = source.WallHeight,
            WallThickness = source.WallThickness,
            FloorColor = source.FloorColor.ToArray(),
            WallColor = source.WallColor.ToArray(),
            Tags = source.Tags.ToList(),
            LootSpawnChance = source.LootSpawnChance,
            MaxLootPerModule = source.MaxLootPerModule,
            HostileSpawnChance = source.HostileSpawnChance,
            MaxHostilesPerModule = source.MaxHostilesPerModule,
            InfectedZoneChance = source.InfectedZoneChance,
            Weight = source.Weight,
            Connections = source.Connections.Select(x => new ConnectionSocketDefinition
            {
                Id = x.Id,
                Direction = x.Direction,
                Kind = x.Kind,
                Offset = x.Offset * multiplier,
                OpeningWidth = x.OpeningWidth * multiplier
            }).ToList(),
            PropSockets = source.PropSockets.Select(x => new PropSocketDefinition
            {
                Id = x.Id,
                SlotType = x.SlotType,
                LocalX = x.LocalX * multiplier,
                LocalY = x.LocalY,
                LocalZ = x.LocalZ * multiplier,
                RotationDegrees = x.RotationDegrees,
                SpawnChance = x.SpawnChance,
                AllowedProps = x.AllowedProps.ToList()
            }).ToList()
        };
    }

    private static PropDefinition CloneProp(PropDefinition source)
    {
        return new PropDefinition
        {
            Id = source.Id,
            SlotType = source.SlotType,
            Size = source.Size.ToArray(),
            Color = source.Color.ToArray()
        };
    }
}

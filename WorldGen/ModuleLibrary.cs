namespace EFP.WorldGen;

public sealed class ModuleLibrary
{
    private readonly Dictionary<string, ModuleDefinition> _modulesById;
    private readonly Dictionary<string, PropDefinition> _propsById;
    private readonly Dictionary<string, List<PropDefinition>> _propsBySlotType;

    public ModuleLibrary(ModuleLibraryConfig config)
    {
        _modulesById = config.Modules.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
        _propsById = config.Props.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
        _propsBySlotType = config.Props
            .GroupBy(x => x.SlotType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
    }

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
        return _propsBySlotType.TryGetValue(slotType, out var props)
            ? props
            : [];
    }
}

using EFP.Model.Catalog;
using EFP.Model.Raid;

namespace EFP.Model.Inventory;

public static class InventoryCatalog
{
    public static IReadOnlyList<InventoryItem> All { get; } =
    [
        new() { Id = "medkit_small",  Label = "Small Medkit",  Width = 2, Height = 1, Value = 30,  Color = [0.86f, 0.24f, 0.26f, 1.0f] },
        new() { Id = "filter_pack",   Label = "Filter Pack",   Width = 1, Height = 1, Value = 18,  Color = [0.42f, 0.66f, 0.78f, 1.0f] },
        new() { Id = "battery_cell",  Label = "Battery Cell",  Width = 1, Height = 2, Value = 25,  Color = [0.86f, 0.70f, 0.22f, 1.0f] },
        new() { Id = "scrap_bundle",  Label = "Scrap Bundle",  Width = 1, Height = 1, Value = 12,  Color = [0.56f, 0.50f, 0.42f, 1.0f] },
        new() { Id = "reagent_vial",  Label = "Reagent Vial",  Width = 1, Height = 2, Value = 35,  Color = [0.36f, 0.92f, 0.68f, 1.0f] },
        new() { Id = "archive_drive", Label = "Archive Drive", Width = 2, Height = 1, Value = 55,  Color = [0.22f, 0.82f, 0.78f, 1.0f] },
        new() { Id = "archive_packet",Label = "Archive Packet",Width = 3, Height = 2, Value = 120, Color = [0.78f, 0.62f, 0.18f, 1.0f] }
    ];

    public static InventoryItem? Resolve(string id)
    {
        foreach (var item in All)
            if (item.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                return item;
        return null;
    }

    public static InventoryItem? ResolveLoot(LootPickup pickup)
    {
        foreach (var def in LootCatalog.All)
            if (pickup.Id.EndsWith(def.Id, StringComparison.OrdinalIgnoreCase))
                return Resolve(def.Id);
        return null;
    }
}

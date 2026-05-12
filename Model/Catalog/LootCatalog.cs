using EFP.Model.Raid;

namespace EFP.Model.Catalog;

public static class LootCatalog
{
    public static IReadOnlyList<LootDefinition> All { get; } =
    [
        new LootDefinition
        {
            Id = "medkit_small",
            Label = "Small Medkit",
            Kind = LootKind.Medkit,
            Value = 0,
            MedkitCount = 1,
            Size = [0.34f, 0.22f, 0.28f],
            Color = [0.86f, 0.24f, 0.26f, 1.0f],
            ModelId = "loot_medkit",
            AllowedArchetypes = ["safe", "residential", "service"],
            Weight = 3.0f
        },
        new LootDefinition
        {
            Id = "filter_pack",
            Label = "Filter Pack",
            Kind = LootKind.Filter,
            Value = 18,
            Size = [0.32f, 0.32f, 0.32f],
            Color = [0.42f, 0.66f, 0.78f, 1.0f],
            ModelId = "loot_filter",
            AllowedArchetypes = ["service", "basement", "corridor"],
            Weight = 4.0f
        },
        new LootDefinition
        {
            Id = "battery_cell",
            Label = "Battery Cell",
            Kind = LootKind.Battery,
            Value = 25,
            Size = [0.28f, 0.42f, 0.28f],
            Color = [0.86f, 0.70f, 0.22f, 1.0f],
            ModelId = "loot_battery",
            AllowedArchetypes = ["service", "basement", "objective"],
            Weight = 3.0f
        },
        new LootDefinition
        {
            Id = "scrap_bundle",
            Label = "Scrap Bundle",
            Kind = LootKind.Scrap,
            Value = 12,
            Size = [0.38f, 0.26f, 0.34f],
            Color = [0.56f, 0.50f, 0.42f, 1.0f],
            ModelId = "loot_scrap",
            AllowedArchetypes = [],
            Weight = 7.0f
        },
        new LootDefinition
        {
            Id = "reagent_vial",
            Label = "Reagent Vial",
            Kind = LootKind.Reagent,
            Value = 35,
            Size = [0.22f, 0.48f, 0.22f],
            Color = [0.36f, 0.92f, 0.68f, 1.0f],
            ModelId = "loot_reagent",
            AllowedArchetypes = ["objective", "basement"],
            Weight = 2.0f
        },
        new LootDefinition
        {
            Id = "archive_drive",
            Label = "Archive Drive",
            Kind = LootKind.Scrap,
            Value = 55,
            Size = [0.42f, 0.18f, 0.32f],
            Color = [0.22f, 0.82f, 0.78f, 1.0f],
            ModelId = "loot_drive",
            AllowedArchetypes = ["objective"],
            Weight = 3.0f
        }
    ];

    public static IReadOnlyList<LootDefinition> ForArchetype(string archetype)
    {
        return All
            .Where(x => x.AllowedArchetypes.Count == 0 ||
                        x.AllowedArchetypes.Any(a => a.Equals(archetype, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public static LootDefinition? PickWeighted(IReadOnlyList<LootDefinition> candidates, Random rng)
    {
        if (candidates.Count == 0) return null;

        var total = candidates.Sum(x => MathF.Max(0.0001f, x.Weight));
        var roll = (float)rng.NextDouble() * total;
        var acc = 0f;
        foreach (var candidate in candidates)
        {
            acc += MathF.Max(0.0001f, candidate.Weight);
            if (roll <= acc) return candidate;
        }

        return candidates[^1];
    }
}

using EFP.Model.Raid;

namespace EFP.Model.Catalog;

public static class HostileCatalog
{
    public static IReadOnlyList<HostileDefinition> All { get; } =
    [
        new HostileDefinition
        {
            Id = "tenant_husk",
            Label = "Tenant Husk",
            MaxHealth = 55f,
            CollisionRadius = 0.34f,
            DormantTint = [0.38f, 0.42f, 0.46f, 1.00f],
            AlertTint = [0.86f, 0.28f, 0.24f, 1.00f],
            Size = [0.72f, 1.14f, 0.72f],
            ModelId = "hostile_husk",
            AllowedArchetypes = ["corridor", "residential", "stair"],
            Weight = 5.0f
        },
        new HostileDefinition
        {
            Id = "service_crawler",
            Label = "Service Crawler",
            MaxHealth = 72f,
            CollisionRadius = 0.38f,
            DormantTint = [0.30f, 0.39f, 0.34f, 1.00f],
            AlertTint = [0.80f, 0.42f, 0.20f, 1.00f],
            Size = [0.95f, 0.78f, 0.95f],
            ModelId = "hostile_crawler",
            AllowedArchetypes = ["service", "basement"],
            Weight = 4.0f
        },
        new HostileDefinition
        {
            Id = "archive_guard",
            Label = "Archive Guard",
            MaxHealth = 105f,
            CollisionRadius = 0.42f,
            DormantTint = [0.34f, 0.32f, 0.42f, 1.00f],
            AlertTint = [0.92f, 0.22f, 0.34f, 1.00f],
            Size = [0.88f, 1.34f, 0.88f],
            ModelId = "hostile_guard",
            AllowedArchetypes = ["objective", "basement"],
            Weight = 2.0f
        },
        new HostileDefinition
        {
            Id = "lost_worker",
            Label = "Lost Worker",
            MaxHealth = 64f,
            CollisionRadius = 0.35f,
            DormantTint = [0.42f, 0.40f, 0.32f, 1.00f],
            AlertTint = [0.84f, 0.58f, 0.22f, 1.00f],
            Size = [0.78f, 1.18f, 0.78f],
            ModelId = "hostile_worker",
            AllowedArchetypes = [],
            Weight = 3.0f
        }
    ];

    public static IReadOnlyList<HostileDefinition> ForArchetype(string archetype)
    {
        return All
            .Where(x => x.AllowedArchetypes.Count == 0 ||
                        x.AllowedArchetypes.Any(a => a.Equals(archetype, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public static HostileDefinition? PickWeighted(IReadOnlyList<HostileDefinition> candidates, Random rng)
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

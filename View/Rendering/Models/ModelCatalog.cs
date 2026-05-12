namespace EFP.View.Rendering.Models;

public static class ModelCatalog
{
    public static IReadOnlyList<ModelDefinition> Defaults { get; } =
    [
        new() { Id = "hostile_husk",    Category = "hostile", Path = "test/hostile_husk.obj" },
        new() { Id = "hostile_crawler", Category = "hostile", Path = "test/hostile_crawler.obj" },
        new() { Id = "hostile_guard",   Category = "hostile", Path = "test/hostile_guard.obj" },
        new() { Id = "hostile_worker",  Category = "hostile", Path = "test/hostile_worker.obj" },
        new() { Id = "loot_medkit",     Category = "loot",    Path = "test/loot_box.obj" },
        new() { Id = "loot_filter",     Category = "loot",    Path = "test/loot_cylinder.obj" },
        new() { Id = "loot_battery",    Category = "loot",    Path = "test/loot_box.obj" },
        new() { Id = "loot_scrap",      Category = "loot",    Path = "test/loot_box.obj" },
        new() { Id = "loot_reagent",    Category = "loot",    Path = "test/loot_cylinder.obj" },
        new() { Id = "loot_drive",      Category = "loot",    Path = "test/loot_box.obj" }
    ];
}

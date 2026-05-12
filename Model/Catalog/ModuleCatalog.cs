using EFP.Model.WorldGen;

namespace EFP.Model.Catalog;

public static class ModuleCatalog
{
    public static IReadOnlyList<ModuleDefinition> Modules { get; } =
    [
        new()
        {
            Id = "safe_room", Archetype = "safe",
            Width = 7.0f, Length = 6.5f, FloorHeight = 0.08f, WallHeight = 3.2f, WallThickness = 0.20f,
            FloorColor = [0.20f, 0.21f, 0.23f, 1.0f], WallColor = [0.58f, 0.60f, 0.62f, 1.0f],
            Tags = ["safe", "start"],
            LootSpawnChance = 0.25f, MaxLootPerModule = 1,
            HostileSpawnChance = 0.0f, MaxHostilesPerModule = 0,
            InfectedZoneChance = 0.0f, Weight = 1,
            Connections =
            [
                new() { Id = "east", Direction = ConnectionDirection.East, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "locker_a", SlotType = "storage", LocalX = -2.2f, LocalZ = -1.8f, AllowedProps = ["locker"], SpawnChance = 1.0f },
                new() { Id = "bench_a", SlotType = "utility", LocalX = -0.2f, LocalZ =  2.0f, AllowedProps = ["bench"], SpawnChance = 1.0f },
                new() { Id = "crate_a", SlotType = "storage", LocalX =  2.2f, LocalZ =  1.8f, AllowedProps = ["crate"], SpawnChance = 0.8f }
            ]
        },
        new()
        {
            Id = "corridor_long", Archetype = "corridor",
            Width = 4.0f, Length = 8.0f, FloorHeight = 0.06f, WallHeight = 3.0f, WallThickness = 0.18f,
            FloorColor = [0.18f, 0.19f, 0.21f, 1.0f], WallColor = [0.54f, 0.56f, 0.58f, 1.0f],
            Tags = ["corridor"],
            LootSpawnChance = 0.35f, MaxLootPerModule = 1,
            HostileSpawnChance = 0.28f, MaxHostilesPerModule = 1,
            InfectedZoneChance = 0.18f, Weight = 7,
            Connections =
            [
                new() { Id = "west", Direction = ConnectionDirection.West, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east", Direction = ConnectionDirection.East, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "pipe_a",  SlotType = "wall",    LocalX =  1.55f, LocalZ = -2.4f, AllowedProps = ["pipe_stack"], SpawnChance = 0.9f },
                new() { Id = "crate_a", SlotType = "storage", LocalX = -1.10f, LocalZ =  2.1f, AllowedProps = ["crate"],      SpawnChance = 0.55f }
            ]
        },
        new()
        {
            Id = "corridor_short", Archetype = "corridor",
            Width = 4.2f, Length = 5.8f, FloorHeight = 0.06f, WallHeight = 3.0f, WallThickness = 0.18f,
            FloorColor = [0.18f, 0.19f, 0.20f, 1.0f], WallColor = [0.53f, 0.55f, 0.57f, 1.0f],
            Tags = ["corridor"],
            LootSpawnChance = 0.30f, MaxLootPerModule = 1,
            HostileSpawnChance = 0.24f, MaxHostilesPerModule = 1,
            InfectedZoneChance = 0.16f, Weight = 6,
            Connections =
            [
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east",  Direction = ConnectionDirection.East,  Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "bench_b", SlotType = "utility", LocalX = -1.25f, LocalZ = 1.3f, AllowedProps = ["bench"], SpawnChance = 0.7f }
            ]
        },
        new()
        {
            Id = "corridor_cross", Archetype = "corridor",
            Width = 6.0f, Length = 6.0f, FloorHeight = 0.06f, WallHeight = 3.0f, WallThickness = 0.18f,
            FloorColor = [0.17f, 0.18f, 0.20f, 1.0f], WallColor = [0.50f, 0.53f, 0.56f, 1.0f],
            Tags = ["corridor", "junction"],
            LootSpawnChance = 0.25f, MaxLootPerModule = 1,
            HostileSpawnChance = 0.35f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.22f, Weight = 5,
            Connections =
            [
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east",  Direction = ConnectionDirection.East,  Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "west",  Direction = ConnectionDirection.West,  Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "pipe_cross", SlotType = "wall", LocalX = 2.1f, LocalZ = -1.4f, AllowedProps = ["pipe_stack"], SpawnChance = 0.75f }
            ]
        },
        new()
        {
            Id = "corridor_t", Archetype = "corridor",
            Width = 6.0f, Length = 5.0f, FloorHeight = 0.06f, WallHeight = 3.0f, WallThickness = 0.18f,
            FloorColor = [0.18f, 0.18f, 0.19f, 1.0f], WallColor = [0.52f, 0.54f, 0.56f, 1.0f],
            Tags = ["corridor", "junction"],
            LootSpawnChance = 0.28f, MaxLootPerModule = 1,
            HostileSpawnChance = 0.32f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.20f, Weight = 6,
            Connections =
            [
                new() { Id = "west",  Direction = ConnectionDirection.West,  Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east",  Direction = ConnectionDirection.East,  Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "trash_a", SlotType = "storage", LocalX = 1.6f, LocalZ = 1.0f, AllowedProps = ["trash_bin"], SpawnChance = 0.7f }
            ]
        },
        new()
        {
            Id = "residential_flat", Archetype = "residential",
            Width = 7.0f, Length = 6.0f, FloorHeight = 0.08f, WallHeight = 3.05f, WallThickness = 0.18f,
            FloorColor = [0.22f, 0.20f, 0.18f, 1.0f], WallColor = [0.62f, 0.58f, 0.50f, 1.0f],
            Tags = ["residential", "room"],
            LootSpawnChance = 0.65f, MaxLootPerModule = 2,
            HostileSpawnChance = 0.40f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.24f, Weight = 5,
            Connections =
            [
                new() { Id = "west", Direction = ConnectionDirection.West, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east", Direction = ConnectionDirection.East, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "sofa_a",  SlotType = "furniture", LocalX = -1.7f, LocalZ = -1.6f, AllowedProps = ["sofa"],  SpawnChance = 0.85f },
                new() { Id = "table_a", SlotType = "utility",   LocalX =  1.6f, LocalZ =  1.4f, AllowedProps = ["table"], SpawnChance = 0.90f }
            ]
        },
        new()
        {
            Id = "residential_kitchen", Archetype = "residential",
            Width = 6.0f, Length = 5.2f, FloorHeight = 0.08f, WallHeight = 3.05f, WallThickness = 0.18f,
            FloorColor = [0.20f, 0.21f, 0.19f, 1.0f], WallColor = [0.60f, 0.61f, 0.54f, 1.0f],
            Tags = ["residential", "kitchen"],
            LootSpawnChance = 0.70f, MaxLootPerModule = 2,
            HostileSpawnChance = 0.38f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.22f, Weight = 4,
            Connections =
            [
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east",  Direction = ConnectionDirection.East,  Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "counter_a", SlotType = "utility", LocalX = -1.7f, LocalZ = -1.3f, AllowedProps = ["counter"], SpawnChance = 1.0f },
                new() { Id = "fridge_a",  SlotType = "storage", LocalX =  1.9f, LocalZ = -1.1f, AllowedProps = ["locker"],  SpawnChance = 0.65f }
            ]
        },
        new()
        {
            Id = "residential_bedroom", Archetype = "residential",
            Width = 5.6f, Length = 5.6f, FloorHeight = 0.08f, WallHeight = 3.05f, WallThickness = 0.18f,
            FloorColor = [0.23f, 0.20f, 0.19f, 1.0f], WallColor = [0.58f, 0.54f, 0.50f, 1.0f],
            Tags = ["residential", "deadend"],
            LootSpawnChance = 0.80f, MaxLootPerModule = 3,
            HostileSpawnChance = 0.32f, MaxHostilesPerModule = 1,
            InfectedZoneChance = 0.18f, Weight = 2,
            Connections =
            [
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "bed_a",     SlotType = "furniture", LocalX = -1.5f, LocalZ = -1.5f, AllowedProps = ["bed"],   SpawnChance = 1.0f },
                new() { Id = "crate_bed", SlotType = "storage",   LocalX =  1.6f, LocalZ =  1.3f, AllowedProps = ["crate"], SpawnChance = 0.65f }
            ]
        },
        new()
        {
            Id = "residential_bathroom", Archetype = "residential",
            Width = 4.4f, Length = 4.8f, FloorHeight = 0.08f, WallHeight = 3.0f, WallThickness = 0.18f,
            FloorColor = [0.19f, 0.22f, 0.23f, 1.0f], WallColor = [0.62f, 0.66f, 0.66f, 1.0f],
            Tags = ["residential", "deadend"],
            LootSpawnChance = 0.55f, MaxLootPerModule = 1,
            HostileSpawnChance = 0.22f, MaxHostilesPerModule = 1,
            InfectedZoneChance = 0.30f, Weight = 2,
            Connections =
            [
                new() { Id = "west", Direction = ConnectionDirection.West, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "sink_a", SlotType = "utility", LocalX = 1.3f, LocalZ = -1.1f, AllowedProps = ["sink"], SpawnChance = 1.0f }
            ]
        },
        new()
        {
            Id = "service_nook", Archetype = "service",
            Width = 5.0f, Length = 4.0f, FloorHeight = 0.06f, WallHeight = 3.0f, WallThickness = 0.18f,
            FloorColor = [0.17f, 0.18f, 0.19f, 1.0f], WallColor = [0.49f, 0.52f, 0.55f, 1.0f],
            Tags = ["service", "power"],
            LootSpawnChance = 0.65f, MaxLootPerModule = 2,
            HostileSpawnChance = 0.42f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.18f, Weight = 3,
            Connections =
            [
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "shelf_a", SlotType = "storage", LocalX = -1.5f, LocalZ = -1.2f, AllowedProps = ["shelf"], SpawnChance = 1.0f },
                new() { Id = "crate_b", SlotType = "storage", LocalX =  1.1f, LocalZ =  0.9f, AllowedProps = ["crate"], SpawnChance = 0.8f }
            ]
        },
        new()
        {
            Id = "service_generator", Archetype = "service",
            Width = 6.5f, Length = 5.2f, FloorHeight = 0.06f, WallHeight = 3.2f, WallThickness = 0.20f,
            FloorColor = [0.16f, 0.17f, 0.18f, 1.0f], WallColor = [0.45f, 0.49f, 0.52f, 1.0f],
            Tags = ["service", "generator"],
            LootSpawnChance = 0.70f, MaxLootPerModule = 2,
            HostileSpawnChance = 0.50f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.24f, Weight = 4,
            Connections =
            [
                new() { Id = "west", Direction = ConnectionDirection.West, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east", Direction = ConnectionDirection.East, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "generator_a", SlotType = "utility", LocalX = -1.4f, LocalZ =  0.0f, AllowedProps = ["generator"],  SpawnChance = 1.0f },
                new() { Id = "pipe_gen",    SlotType = "wall",    LocalX =  2.0f, LocalZ = -1.5f, AllowedProps = ["pipe_stack"], SpawnChance = 0.8f }
            ]
        },
        new()
        {
            Id = "utility_pump_room", Archetype = "service",
            Width = 5.8f, Length = 6.2f, FloorHeight = 0.06f, WallHeight = 3.1f, WallThickness = 0.20f,
            FloorColor = [0.15f, 0.17f, 0.18f, 1.0f], WallColor = [0.43f, 0.48f, 0.50f, 1.0f],
            Tags = ["service", "pump"],
            LootSpawnChance = 0.60f, MaxLootPerModule = 2,
            HostileSpawnChance = 0.46f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.28f, Weight = 3,
            Connections =
            [
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "pump_a",     SlotType = "utility", LocalX = -1.6f, LocalZ = -1.3f, AllowedProps = ["generator"], SpawnChance = 0.85f },
                new() { Id = "shelf_pump", SlotType = "storage", LocalX =  1.6f, LocalZ =  1.5f, AllowedProps = ["shelf"],     SpawnChance = 0.8f }
            ]
        },
        new()
        {
            Id = "stair_hall", Archetype = "stair",
            Width = 6.0f, Length = 6.0f, FloorHeight = 0.08f, WallHeight = 3.2f, WallThickness = 0.20f,
            FloorColor = [0.19f, 0.20f, 0.22f, 1.0f], WallColor = [0.56f, 0.58f, 0.60f, 1.0f],
            Tags = ["stair", "transition"],
            LootSpawnChance = 0.40f, MaxLootPerModule = 1,
            HostileSpawnChance = 0.35f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.20f, Weight = 4,
            Connections =
            [
                new() { Id = "west",  Direction = ConnectionDirection.West,  Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "stair_block", SlotType = "utility", LocalX = -1.2f, LocalZ = 0.4f, AllowedProps = ["stair_block"], SpawnChance = 1.0f }
            ]
        },
        new()
        {
            Id = "stairwell_split", Archetype = "stair",
            Width = 6.4f, Length = 7.0f, FloorHeight = 0.08f, WallHeight = 3.3f, WallThickness = 0.20f,
            FloorColor = [0.18f, 0.19f, 0.21f, 1.0f], WallColor = [0.53f, 0.55f, 0.58f, 1.0f],
            Tags = ["stair", "junction"],
            LootSpawnChance = 0.35f, MaxLootPerModule = 1,
            HostileSpawnChance = 0.38f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.20f, Weight = 4,
            Connections =
            [
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east",  Direction = ConnectionDirection.East,  Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "west",  Direction = ConnectionDirection.West,  Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "pipe_stair", SlotType = "wall", LocalX = 2.1f, LocalZ = -1.8f, AllowedProps = ["pipe_stack"], SpawnChance = 0.9f }
            ]
        },
        new()
        {
            Id = "basement_storage", Archetype = "basement",
            Width = 7.0f, Length = 6.5f, FloorHeight = 0.06f, WallHeight = 2.9f, WallThickness = 0.22f,
            FloorColor = [0.14f, 0.15f, 0.16f, 1.0f], WallColor = [0.38f, 0.42f, 0.44f, 1.0f],
            Tags = ["basement", "storage"],
            LootSpawnChance = 0.78f, MaxLootPerModule = 3,
            HostileSpawnChance = 0.55f, MaxHostilesPerModule = 3,
            InfectedZoneChance = 0.35f, Weight = 4,
            Connections =
            [
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east",  Direction = ConnectionDirection.East,  Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "shelf_store", SlotType = "storage", LocalX = -2.0f, LocalZ = -1.7f, AllowedProps = ["shelf"], SpawnChance = 0.85f },
                new() { Id = "crate_store", SlotType = "storage", LocalX =  1.8f, LocalZ =  1.6f, AllowedProps = ["crate"], SpawnChance = 0.8f }
            ]
        },
        new()
        {
            Id = "basement_tunnel", Archetype = "basement",
            Width = 4.6f, Length = 8.5f, FloorHeight = 0.06f, WallHeight = 2.8f, WallThickness = 0.22f,
            FloorColor = [0.13f, 0.14f, 0.15f, 1.0f], WallColor = [0.35f, 0.39f, 0.41f, 1.0f],
            Tags = ["basement", "corridor"],
            LootSpawnChance = 0.48f, MaxLootPerModule = 2,
            HostileSpawnChance = 0.48f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.32f, Weight = 5,
            Connections =
            [
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "pipe_tunnel_a", SlotType = "wall", LocalX = -1.6f, LocalZ = -2.5f, AllowedProps = ["pipe_stack"], SpawnChance = 0.9f }
            ]
        },
        new()
        {
            Id = "basement_boiler", Archetype = "basement",
            Width = 7.2f, Length = 6.2f, FloorHeight = 0.06f, WallHeight = 3.1f, WallThickness = 0.22f,
            FloorColor = [0.15f, 0.14f, 0.13f, 1.0f], WallColor = [0.42f, 0.38f, 0.34f, 1.0f],
            Tags = ["basement", "boiler"],
            LootSpawnChance = 0.62f, MaxLootPerModule = 2,
            HostileSpawnChance = 0.58f, MaxHostilesPerModule = 3,
            InfectedZoneChance = 0.38f, Weight = 3,
            Connections =
            [
                new() { Id = "west",  Direction = ConnectionDirection.West,  Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "boiler_a",    SlotType = "utility", LocalX = -1.8f, LocalZ =  0.0f, AllowedProps = ["generator"],  SpawnChance = 0.9f },
                new() { Id = "pipe_boiler", SlotType = "wall",    LocalX =  2.1f, LocalZ = -1.5f, AllowedProps = ["pipe_stack"], SpawnChance = 0.9f }
            ]
        },
        new()
        {
            Id = "laundry_room", Archetype = "residential",
            Width = 6.2f, Length = 5.4f, FloorHeight = 0.08f, WallHeight = 3.0f, WallThickness = 0.18f,
            FloorColor = [0.18f, 0.20f, 0.21f, 1.0f], WallColor = [0.55f, 0.60f, 0.61f, 1.0f],
            Tags = ["residential", "laundry"],
            LootSpawnChance = 0.58f, MaxLootPerModule = 2,
            HostileSpawnChance = 0.36f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.26f, Weight = 3,
            Connections =
            [
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "east",  Direction = ConnectionDirection.East,  Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "washer_a", SlotType = "utility", LocalX = -1.7f, LocalZ = -1.4f, AllowedProps = ["counter"],   SpawnChance = 0.8f },
                new() { Id = "basket_a", SlotType = "storage", LocalX =  1.5f, LocalZ =  1.3f, AllowedProps = ["trash_bin"], SpawnChance = 0.7f }
            ]
        },
        new()
        {
            Id = "trash_chute", Archetype = "service",
            Width = 4.8f, Length = 5.4f, FloorHeight = 0.06f, WallHeight = 3.0f, WallThickness = 0.20f,
            FloorColor = [0.16f, 0.16f, 0.15f, 1.0f], WallColor = [0.44f, 0.44f, 0.40f, 1.0f],
            Tags = ["service", "trash"],
            LootSpawnChance = 0.52f, MaxLootPerModule = 2,
            HostileSpawnChance = 0.44f, MaxHostilesPerModule = 2,
            InfectedZoneChance = 0.34f, Weight = 3,
            Connections =
            [
                new() { Id = "north", Direction = ConnectionDirection.North, Kind = "doorway", OpeningWidth = 2.0f },
                new() { Id = "west",  Direction = ConnectionDirection.West,  Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "trash_chute_a", SlotType = "utility", LocalX = 1.3f, LocalZ = -1.2f, AllowedProps = ["trash_bin"], SpawnChance = 1.0f }
            ]
        },
        new()
        {
            Id = "objective_room", Archetype = "objective",
            Width = 8.0f, Length = 7.0f, FloorHeight = 0.08f, WallHeight = 3.4f, WallThickness = 0.22f,
            FloorColor = [0.17f, 0.18f, 0.19f, 1.0f], WallColor = [0.60f, 0.62f, 0.65f, 1.0f],
            Tags = ["objective", "archive"],
            LootSpawnChance = 1.00f, MaxLootPerModule = 4,
            HostileSpawnChance = 0.82f, MaxHostilesPerModule = 3,
            InfectedZoneChance = 0.40f, Weight = 2,
            Connections =
            [
                new() { Id = "west", Direction = ConnectionDirection.West, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "archive_a", SlotType = "storage", LocalX = -2.5f, LocalZ = -2.0f, AllowedProps = ["archive_shelf"], SpawnChance = 1.0f },
                new() { Id = "archive_b", SlotType = "storage", LocalX =  2.5f, LocalZ = -2.0f, AllowedProps = ["archive_shelf"], SpawnChance = 1.0f },
                new() { Id = "console_a", SlotType = "utility", LocalX =  0.0f, LocalZ =  1.6f, AllowedProps = ["console"],       SpawnChance = 1.0f }
            ]
        },
        new()
        {
            Id = "objective_archive_deep", Archetype = "objective",
            Width = 7.5f, Length = 8.0f, FloorHeight = 0.08f, WallHeight = 3.4f, WallThickness = 0.22f,
            FloorColor = [0.16f, 0.17f, 0.19f, 1.0f], WallColor = [0.56f, 0.59f, 0.63f, 1.0f],
            Tags = ["objective", "archive", "deadend"],
            LootSpawnChance = 1.00f, MaxLootPerModule = 4,
            HostileSpawnChance = 0.88f, MaxHostilesPerModule = 4,
            InfectedZoneChance = 0.45f, Weight = 1,
            Connections =
            [
                new() { Id = "south", Direction = ConnectionDirection.South, Kind = "doorway", OpeningWidth = 2.0f }
            ],
            PropSockets =
            [
                new() { Id = "archive_deep_a", SlotType = "storage", LocalX = -2.2f, LocalZ = -2.4f, AllowedProps = ["archive_shelf"], SpawnChance = 1.0f },
                new() { Id = "archive_deep_b", SlotType = "storage", LocalX =  2.2f, LocalZ = -2.4f, AllowedProps = ["archive_shelf"], SpawnChance = 1.0f },
                new() { Id = "console_deep",   SlotType = "utility", LocalX =  0.0f, LocalZ =  2.0f, AllowedProps = ["console"],       SpawnChance = 1.0f }
            ]
        }
    ];

    public static IReadOnlyList<PropDefinition> Props { get; } =
    [
        new() { Id = "crate",         SlotType = "storage",   Size = [0.9f, 0.7f,  0.9f],  Color = [0.44f, 0.31f, 0.18f, 1.0f] },
        new() { Id = "locker",        SlotType = "storage",   Size = [0.8f, 1.9f,  0.45f], Color = [0.42f, 0.50f, 0.56f, 1.0f] },
        new() { Id = "shelf",         SlotType = "storage",   Size = [1.6f, 1.8f,  0.45f], Color = [0.47f, 0.39f, 0.27f, 1.0f] },
        new() { Id = "archive_shelf", SlotType = "storage",   Size = [1.8f, 2.1f,  0.5f],  Color = [0.54f, 0.46f, 0.31f, 1.0f] },
        new() { Id = "bench",         SlotType = "utility",   Size = [1.5f, 0.55f, 0.45f], Color = [0.38f, 0.33f, 0.24f, 1.0f] },
        new() { Id = "console",       SlotType = "utility",   Size = [1.4f, 1.0f,  0.7f],  Color = [0.26f, 0.43f, 0.45f, 1.0f] },
        new() { Id = "stair_block",   SlotType = "utility",   Size = [2.4f, 1.1f,  2.0f],  Color = [0.36f, 0.37f, 0.39f, 1.0f] },
        new() { Id = "pipe_stack",    SlotType = "wall",      Size = [0.35f, 2.2f, 0.35f], Color = [0.51f, 0.58f, 0.29f, 1.0f] },
        new() { Id = "sofa",          SlotType = "furniture", Size = [2.0f, 0.7f,  0.85f], Color = [0.32f, 0.36f, 0.42f, 1.0f] },
        new() { Id = "table",         SlotType = "utility",   Size = [1.3f, 0.75f, 0.9f],  Color = [0.42f, 0.31f, 0.20f, 1.0f] },
        new() { Id = "bed",           SlotType = "furniture", Size = [2.0f, 0.55f, 1.3f],  Color = [0.36f, 0.34f, 0.38f, 1.0f] },
        new() { Id = "counter",       SlotType = "utility",   Size = [1.8f, 0.95f, 0.55f], Color = [0.48f, 0.50f, 0.46f, 1.0f] },
        new() { Id = "sink",          SlotType = "utility",   Size = [0.8f, 0.85f, 0.55f], Color = [0.66f, 0.69f, 0.68f, 1.0f] },
        new() { Id = "generator",     SlotType = "utility",   Size = [1.7f, 1.25f, 1.05f], Color = [0.28f, 0.32f, 0.34f, 1.0f] },
        new() { Id = "trash_bin",     SlotType = "storage",   Size = [0.75f, 0.9f, 0.75f], Color = [0.26f, 0.29f, 0.25f, 1.0f] }
    ];
}

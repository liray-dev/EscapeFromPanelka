using System.Numerics;
using EFP.Entities;
using EFP.World;

namespace EFP.WorldGen;

public sealed class StructureAssembler
{
    public static ProceduralSector Assemble(StructureBlueprint blueprint, ModuleLibrary library)
    {
        var sector = new ProceduralSector
        {
            Seed = blueprint.Seed,
            RoomSizeMultiplier = library.RoomSizeMultiplier
        };
        var rng = new Random(blueprint.Seed);
        var placedByNodeId = new Dictionary<string, PlacedModule>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in blueprint.Steps)
        {
            var definition = library.GetModule(step.ModuleId);
            PlacedModule placedModule;

            if (string.IsNullOrWhiteSpace(step.ParentNodeId))
            {
                placedModule = new PlacedModule(step.NodeId, definition, Vector3.Zero, 0f, step.MainRoute);
            }
            else
            {
                var parent = placedByNodeId[step.ParentNodeId];
                var childSocket = definition.Connections.FirstOrDefault(x =>
                                      x.Id.Equals(step.ChildSocketId, StringComparison.OrdinalIgnoreCase))
                                  ?? throw new InvalidOperationException(
                                      $"Child socket {step.ChildSocketId} not found in {definition.Id}");

                var parentDirection = parent.GetWorldSocketDirection(step.ParentSocketId);
                var desiredChildDirection = WorldGenMath.Opposite(parentDirection);
                var childRotation = WorldGenMath.SolveRotationDegrees(childSocket.Direction, desiredChildDirection);

                var parentWorldSocketPosition = parent.GetWorldSocketPosition(step.ParentSocketId);
                var rotatedChildSocketLocal = RotateLocal(WorldGenMath.GetSocketLocalPosition(definition, childSocket),
                    childRotation);
                var childPosition = parentWorldSocketPosition - rotatedChildSocketLocal;

                placedModule = new PlacedModule(step.NodeId, definition, childPosition, childRotation, step.MainRoute);
            }

            sector.Modules.Add(placedModule);
            placedByNodeId[step.NodeId] = placedModule;
        }

        var safeBlock = placedByNodeId["safe_block"];
        var hallA = placedByNodeId["hall_a"];
        var serviceNook = placedByNodeId["service_nook"];
        var hallB = placedByNodeId["hall_b"];
        var objectiveRoom = placedByNodeId["objective_room"];
        var scale = sector.RoomSizeMultiplier;

        sector.SafeBlockCenter = safeBlock.Position;
        sector.ExtractionConsolePoint =
            safeBlock.ToWorldPosition(ScalePlanar(new Vector3(2.15f, 0.95f, -1.25f), scale));
        sector.PowerSwitchPoint = serviceNook.ToWorldPosition(ScalePlanar(new Vector3(2.05f, 0.95f, -1.10f), scale));
        sector.ObjectivePoint = objectiveRoom.Position + new Vector3(0f, 0.85f, 0f);
        sector.PlayerSpawn = sector.SafeBlockCenter + new Vector3(0f, 0.5f, 0f);

        BuildGeometry(sector);
        BuildFeatures(sector, safeBlock, hallA, serviceNook, hallB, objectiveRoom, scale);
        SpawnProps(sector, library, rng);
        BuildLights(sector);
        BuildInfectedZones(sector, hallA, hallB, objectiveRoom, scale);
        BuildHostiles(sector, hallB, objectiveRoom, scale);
        BuildLoot(sector, safeBlock, hallA, serviceNook, hallB, objectiveRoom, scale);
        ComputeBounds(sector);
        return sector;
    }

    private static void BuildGeometry(ProceduralSector sector)
    {
        foreach (var module in sector.Modules)
        {
            AddFloor(sector.StaticGeometry, module);
            AddPerimeterWalls(sector.StaticGeometry, module);
        }
    }

    private static void BuildFeatures(ProceduralSector sector, PlacedModule safeBlock, PlacedModule hallA,
        PlacedModule serviceNook, PlacedModule hallB, PlacedModule objectiveRoom, float scale)
    {
        var extractionConsole = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                safeBlock,
                ScalePlanar(new Vector3(2.15f, 0.52f, -1.25f), scale),
                Vector3.Zero,
                ScalePlanar(new Vector3(0.38f, 1.04f, 0.24f), scale)),
            new Vector4(0.32f, 0.58f, 0.64f, 1f));

        var powerConsole = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                serviceNook,
                ScalePlanar(new Vector3(2.05f, 0.56f, -1.10f), scale),
                Vector3.Zero,
                ScalePlanar(new Vector3(0.36f, 1.12f, 0.24f), scale)),
            new Vector4(0.64f, 0.52f, 0.18f, 1f));

        sector.FeatureGeometry.Add(extractionConsole);
        sector.FeatureGeometry.Add(powerConsole);

        var serviceDoor = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                hallA,
                new Vector3(0.8f * scale, 1.12f,
                    -hallA.Definition.Length * 0.5f + hallA.Definition.WallThickness * 0.5f),
                Vector3.Zero,
                new Vector3(1.82f * scale, 2.24f, hallA.Definition.WallThickness)),
            new Vector4(0.54f, 0.58f, 0.62f, 1f));

        var archiveBulkhead = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                hallB,
                new Vector3(hallB.Definition.Width * 0.5f - hallB.Definition.WallThickness * 0.5f, 1.15f, 0f),
                Vector3.Zero,
                new Vector3(hallB.Definition.WallThickness + 0.14f, 2.30f, 1.92f * scale)),
            new Vector4(0.66f, 0.20f, 0.18f, 1f));

        var quarantineShutter = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                hallA,
                new Vector3(0.8f * scale, 1.15f,
                    -hallA.Definition.Length * 0.5f + hallA.Definition.WallThickness * 0.5f - 0.24f),
                Vector3.Zero,
                new Vector3(2.10f * scale, 2.30f, 0.28f)),
            new Vector4(0.62f, 0.14f, 0.22f, 1f));

        sector.LockablePassages.Add(new LockablePassage(
            "service_hatch",
            string.Empty,
            "Сервисную дверь",
            serviceDoor,
            DoorState.Closed));

        sector.LockablePassages.Add(new LockablePassage(
            "bulkhead_archive",
            "service_power",
            "Аварийную переборку к архиву",
            archiveBulkhead,
            DoorState.Locked));

        sector.LockablePassages.Add(new LockablePassage(
            "quarantine_shutter",
            string.Empty,
            "Карантинную шторку",
            quarantineShutter,
            DoorState.Open,
            true));

        sector.CriticalMutationGeometry.Add(new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                hallA,
                new Vector3(0.4f * scale, 0.62f, -hallA.Definition.Length * 0.5f + 1.05f * scale),
                Vector3.Zero,
                new Vector3(2.85f * scale, 1.22f, 1.45f * scale)),
            new Vector4(0.42f, 0.11f, 0.22f, 1f)));

        sector.CriticalMutationGeometry.Add(new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                hallA,
                new Vector3(1.35f * scale, 1.66f, -hallA.Definition.Length * 0.5f + 1.15f * scale),
                Vector3.Zero,
                new Vector3(1.10f * scale, 0.92f, 0.86f * scale)),
            new Vector4(0.54f, 0.14f, 0.25f, 1f)));

        sector.CriticalMutationGeometry.Add(new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                objectiveRoom,
                ScalePlanar(new Vector3(2.4f, 0.72f, -2.1f), scale),
                Vector3.Zero,
                ScalePlanar(new Vector3(1.28f, 1.44f, 1.05f), scale)),
            new Vector4(0.36f, 0.10f, 0.20f, 1f)));
    }

    private static void BuildLights(ProceduralSector sector)
    {
        var index = 0;
        foreach (var module in sector.Modules)
        {
            var lightLocalPosition = new Vector3(0f, module.Definition.WallHeight - 0.42f, 0f);
            var lightPosition = module.ToWorldPosition(lightLocalPosition);
            var fixtureScale = new Vector3(0.32f, 0.14f, 0.32f);
            var fixtureTint = ResolveLightFixtureTint(module.Definition.Archetype);
            var lightColor = ResolveLightColor(module.Definition.Archetype);
            var radius = ResolveLightRadius(module.Definition.Archetype, module.Definition.Width,
                module.Definition.Length);
            var intensity = ResolveLightIntensity(module.Definition.Archetype);
            var flickerSpeed = module.Definition.Archetype is "service" or "objective" ? 4.6f : 0.8f;
            var emergency = module.Definition.Archetype is not "safe";

            sector.FeatureGeometry.Add(new WorldRenderable(
                WorldPrimitiveType.Cube,
                new Transform
                {
                    Position = lightPosition,
                    Scale = fixtureScale
                },
                fixtureTint));

            sector.Lights.Add(new WorldLight(
                $"light_{module.NodeId}",
                lightPosition,
                lightColor,
                radius,
                intensity,
                flickerSpeed,
                index * 0.71f,
                emergency));

            index++;
        }
    }

    private static void BuildInfectedZones(ProceduralSector sector, PlacedModule hallA, PlacedModule hallB,
        PlacedModule objectiveRoom, float scale)
    {
        var hallZoneCenter = hallB.ToWorldPosition(ScalePlanar(new Vector3(0.2f, 0.02f, 0f), scale));
        var hallZoneRenderable = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = hallZoneCenter + new Vector3(0f, 0.03f, 0f),
                Scale = new Vector3(2.8f * scale, 0.06f, 2.0f * scale)
            },
            new Vector4(0.42f, 0.08f, 0.14f, 1f));

        var objectiveZoneCenter = objectiveRoom.ToWorldPosition(ScalePlanar(new Vector3(-0.8f, 0.02f, 0.6f), scale));
        var objectiveZoneRenderable = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = objectiveZoneCenter + new Vector3(0f, 0.03f, 0f),
                Scale = new Vector3(3.0f * scale, 0.06f, 2.6f * scale)
            },
            new Vector4(0.52f, 0.10f, 0.18f, 1f));

        var corridorZoneCenter = hallA.ToWorldPosition(ScalePlanar(new Vector3(1.0f, 0.02f, 1.8f), scale));
        var corridorZoneRenderable = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = corridorZoneCenter + new Vector3(0f, 0.03f, 0f),
                Scale = new Vector3(2.2f * scale, 0.06f, 1.8f * scale)
            },
            new Vector4(0.34f, 0.08f, 0.13f, 1f));

        sector.InfectedZones.Add(new InfectedZone(
            "infected_hall_b",
            "Архивный коридор затягивает слизью",
            hallZoneCenter,
            1.55f * scale,
            RaidPressureLevel.Pressure,
            0.78f,
            7.5f,
            0.12f,
            hallZoneRenderable));

        sector.InfectedZones.Add(new InfectedZone(
            "infected_objective",
            "Архивная комната заражена",
            objectiveZoneCenter,
            1.85f * scale,
            RaidPressureLevel.Pressure,
            0.68f,
            10.0f,
            0.16f,
            objectiveZoneRenderable));

        sector.InfectedZones.Add(new InfectedZone(
            "infected_hall_a",
            "Коридор режет фиолетовым туманом",
            corridorZoneCenter,
            1.25f * scale,
            RaidPressureLevel.Critical,
            0.72f,
            13.0f,
            0.18f,
            corridorZoneRenderable));
    }

    private static void BuildHostiles(ProceduralSector sector, PlacedModule hallB, PlacedModule objectiveRoom,
        float scale)
    {
        sector.Hostiles.Add(new HostileEntity(
            "lurker_hall",
            "Лестничный обитатель",
            hallB.ToWorldPosition(ScalePlanar(new Vector3(-0.7f, 0f, 0.7f), scale)),
            new Vector4(0.34f, 0.37f, 0.42f, 1f),
            new Vector4(0.72f, 0.28f, 0.34f, 1f)));

        sector.Hostiles.Add(new HostileEntity(
            "lurker_archive",
            "Архивная тварь",
            objectiveRoom.ToWorldPosition(ScalePlanar(new Vector3(1.5f, 0f, -1.0f), scale)),
            new Vector4(0.28f, 0.32f, 0.30f, 1f),
            new Vector4(0.82f, 0.24f, 0.30f, 1f)));
    }


    private static void BuildLoot(ProceduralSector sector, PlacedModule safeBlock, PlacedModule hallA,
        PlacedModule serviceNook, PlacedModule hallB, PlacedModule objectiveRoom, float scale)
    {
        sector.Loot.Add(CreateLootPickup(
            "medkit_service",
            "аптечка ликвидатора",
            LootKind.Medkit,
            serviceNook,
            ScalePlanar(new Vector3(-1.35f, 0.38f, 1.10f), scale),
            new Vector3(0.26f, 0.26f, 0.26f),
            new Vector4(0.26f, 0.72f, 0.46f, 1f),
            0,
            1));

        sector.Loot.Add(CreateLootPickup(
            "filter_corridor",
            "фильтр грубой очистки",
            LootKind.Filter,
            hallA,
            ScalePlanar(new Vector3(-1.10f, 0.34f, 1.05f), scale),
            new Vector3(0.24f, 0.22f, 0.24f),
            new Vector4(0.68f, 0.74f, 0.30f, 1f),
            28));

        sector.Loot.Add(CreateLootPickup(
            "battery_stair",
            "аварийный аккумулятор",
            LootKind.Battery,
            hallB,
            ScalePlanar(new Vector3(0.95f, 0.34f, -1.10f), scale),
            new Vector3(0.24f, 0.24f, 0.24f),
            new Vector4(0.30f, 0.64f, 0.76f, 1f),
            36));

        sector.Loot.Add(CreateLootPickup(
            "scrap_archive",
            "ящик с редкими реагентами",
            LootKind.Reagent,
            objectiveRoom,
            ScalePlanar(new Vector3(2.15f, 0.36f, 1.35f), scale),
            new Vector3(0.30f, 0.30f, 0.30f),
            new Vector4(0.70f, 0.40f, 0.82f, 1f),
            54));

        sector.Loot.Add(CreateLootPickup(
            "scrap_safe",
            "контейнер с ломом",
            LootKind.Scrap,
            safeBlock,
            ScalePlanar(new Vector3(-1.75f, 0.34f, 1.40f), scale),
            new Vector3(0.24f, 0.24f, 0.24f),
            new Vector4(0.66f, 0.52f, 0.32f, 1f),
            18));
    }

    private static LootPickup CreateLootPickup(string id, string label, LootKind kind, PlacedModule module,
        Vector3 localPosition, Vector3 scale, Vector4 tint, int value, int medkitCount = 0)
    {
        var transform = CreateWorldTransform(module, localPosition, Vector3.Zero, scale);
        var renderable = new WorldRenderable(WorldPrimitiveType.Cube, transform, tint);
        return new LootPickup(id, label, kind, transform.Position, renderable, value, medkitCount);
    }

    private static void AddFloor(List<WorldRenderable> geometry, PlacedModule module)
    {
        var floorColor = ToTint(module.Definition.FloorColor);
        var localPosition = new Vector3(0f, module.Definition.FloorHeight * 0.5f, 0f);
        var floorTransform = CreateWorldTransform(
            module,
            localPosition,
            Vector3.Zero,
            new Vector3(module.Definition.Width, module.Definition.FloorHeight, module.Definition.Length));

        geometry.Add(new WorldRenderable(WorldPrimitiveType.Cube, floorTransform, floorColor));
    }

    private static void AddPerimeterWalls(List<WorldRenderable> geometry, PlacedModule module)
    {
        AddEdgeWalls(geometry, module, ConnectionDirection.North);
        AddEdgeWalls(geometry, module, ConnectionDirection.South);
        AddEdgeWalls(geometry, module, ConnectionDirection.East);
        AddEdgeWalls(geometry, module, ConnectionDirection.West);
    }

    private static void AddEdgeWalls(List<WorldRenderable> geometry, PlacedModule module, ConnectionDirection direction)
    {
        var definition = module.Definition;
        var wallColor = ToTint(definition.WallColor);
        var edgeLength = direction is ConnectionDirection.North or ConnectionDirection.South
            ? definition.Width
            : definition.Length;
        var halfEdge = edgeLength * 0.5f;
        var thickness = definition.WallThickness;
        var wallHeight = definition.WallHeight;
        var topBeamHeight = MathF.Min(0.7f, wallHeight * 0.28f);

        var openings = definition.Connections
            .Where(x => x.Direction == direction)
            .Select(x => (Start: x.Offset - x.OpeningWidth * 0.5f, End: x.Offset + x.OpeningWidth * 0.5f,
                Width: x.OpeningWidth, Center: x.Offset))
            .OrderBy(x => x.Start)
            .ToList();

        var current = -halfEdge;
        foreach (var opening in openings)
        {
            if (opening.Start > current + 0.01f)
                AddWallSegment(geometry, module, direction, current, opening.Start, wallHeight, thickness, wallColor);

            AddOpeningTop(geometry, module, direction, opening.Center, opening.Width, wallHeight, topBeamHeight,
                thickness, wallColor);
            current = opening.End;
        }

        if (current < halfEdge - 0.01f)
            AddWallSegment(geometry, module, direction, current, halfEdge, wallHeight, thickness, wallColor);
    }

    private static void AddWallSegment(List<WorldRenderable> geometry, PlacedModule module,
        ConnectionDirection direction, float from, float to, float wallHeight, float thickness, Vector4 tint)
    {
        var length = to - from;
        if (length <= 0.01f) return;

        var center = (from + to) * 0.5f;
        Vector3 localPosition;
        Vector3 localScale;

        switch (direction)
        {
            case ConnectionDirection.North:
                localPosition = new Vector3(center, wallHeight * 0.5f,
                    -module.Definition.Length * 0.5f + thickness * 0.5f);
                localScale = new Vector3(length, wallHeight, thickness);
                break;
            case ConnectionDirection.South:
                localPosition = new Vector3(center, wallHeight * 0.5f,
                    module.Definition.Length * 0.5f - thickness * 0.5f);
                localScale = new Vector3(length, wallHeight, thickness);
                break;
            case ConnectionDirection.East:
                localPosition = new Vector3(module.Definition.Width * 0.5f - thickness * 0.5f, wallHeight * 0.5f,
                    center);
                localScale = new Vector3(thickness, wallHeight, length);
                break;
            case ConnectionDirection.West:
                localPosition = new Vector3(-module.Definition.Width * 0.5f + thickness * 0.5f, wallHeight * 0.5f,
                    center);
                localScale = new Vector3(thickness, wallHeight, length);
                break;
            default:
                return;
        }

        geometry.Add(new WorldRenderable(WorldPrimitiveType.Cube,
            CreateWorldTransform(module, localPosition, Vector3.Zero, localScale), tint));
    }

    private static void AddOpeningTop(List<WorldRenderable> geometry, PlacedModule module,
        ConnectionDirection direction, float openingCenter, float openingWidth, float wallHeight, float topBeamHeight,
        float thickness, Vector4 tint)
    {
        Vector3 localPosition;
        Vector3 localScale;

        switch (direction)
        {
            case ConnectionDirection.North:
                localPosition = new Vector3(openingCenter, wallHeight - topBeamHeight * 0.5f,
                    -module.Definition.Length * 0.5f + thickness * 0.5f);
                localScale = new Vector3(openingWidth, topBeamHeight, thickness);
                break;
            case ConnectionDirection.South:
                localPosition = new Vector3(openingCenter, wallHeight - topBeamHeight * 0.5f,
                    module.Definition.Length * 0.5f - thickness * 0.5f);
                localScale = new Vector3(openingWidth, topBeamHeight, thickness);
                break;
            case ConnectionDirection.East:
                localPosition = new Vector3(module.Definition.Width * 0.5f - thickness * 0.5f,
                    wallHeight - topBeamHeight * 0.5f, openingCenter);
                localScale = new Vector3(thickness, topBeamHeight, openingWidth);
                break;
            case ConnectionDirection.West:
                localPosition = new Vector3(-module.Definition.Width * 0.5f + thickness * 0.5f,
                    wallHeight - topBeamHeight * 0.5f, openingCenter);
                localScale = new Vector3(thickness, topBeamHeight, openingWidth);
                break;
            default:
                return;
        }

        geometry.Add(new WorldRenderable(WorldPrimitiveType.Cube,
            CreateWorldTransform(module, localPosition, Vector3.Zero, localScale), tint));
    }

    private static void SpawnProps(ProceduralSector sector, ModuleLibrary library, Random rng)
    {
        foreach (var module in sector.Modules)
        foreach (var socket in module.Definition.PropSockets)
        {
            if (rng.NextDouble() > socket.SpawnChance) continue;

            var propDefinition = ResolvePropDefinition(library, socket, rng);
            if (propDefinition is null) continue;

            var localPosition =
                new Vector3(socket.LocalX, socket.LocalY + propDefinition.Size[1] * 0.5f, socket.LocalZ);
            var localRotation = new Vector3(0f, MathF.PI / 180f * socket.RotationDegrees, 0f);
            var scale = new Vector3(propDefinition.Size[0], propDefinition.Size[1], propDefinition.Size[2]);
            var transform = CreateWorldTransform(module, localPosition, localRotation, scale);
            var tint = ToTint(propDefinition.Color);
            var renderable = new WorldRenderable(WorldPrimitiveType.Cube, transform, tint);

            sector.Props.Add(new PropInstance(propDefinition.Id, module.NodeId, renderable));
        }
    }

    private static PropDefinition? ResolvePropDefinition(ModuleLibrary library, PropSocketDefinition socket, Random rng)
    {
        if (socket.AllowedProps.Count > 0)
        {
            var propId = socket.AllowedProps[rng.Next(socket.AllowedProps.Count)];
            return library.GetProp(propId);
        }

        var candidates = library.GetPropsForSlot(socket.SlotType);
        if (candidates.Count == 0) return null;

        return candidates[rng.Next(candidates.Count)];
    }

    private static void ComputeBounds(ProceduralSector sector)
    {
        if (sector.Modules.Count == 0)
        {
            sector.BoundsMin = new Vector3(-8f, 0f, -8f);
            sector.BoundsMax = new Vector3(8f, 4f, 8f);
            return;
        }

        var min = new Vector3(float.MaxValue, 0f, float.MaxValue);
        var max = new Vector3(float.MinValue, 0f, float.MinValue);

        foreach (var module in sector.Modules)
        {
            var halfWidth = module.Definition.Width * 0.5f;
            var halfLength = module.Definition.Length * 0.5f;
            var corners = new[]
            {
                new Vector3(-halfWidth, 0f, -halfLength),
                new Vector3(halfWidth, 0f, -halfLength),
                new Vector3(halfWidth, 0f, halfLength),
                new Vector3(-halfWidth, 0f, halfLength)
            };

            foreach (var corner in corners)
            {
                var worldCorner = module.ToWorldPosition(corner);
                min = Vector3.Min(min, worldCorner);
                max = Vector3.Max(max, worldCorner);
            }
        }

        sector.BoundsMin = min - new Vector3(4f, 0f, 4f);
        sector.BoundsMax = max + new Vector3(4f, 4f, 4f);
    }

    private static Transform CreateWorldTransform(PlacedModule module, Vector3 localPosition,
        Vector3 localRotationRadians, Vector3 localScale)
    {
        return new Transform
        {
            Position = module.ToWorldPosition(localPosition),
            Rotation = new Vector3(localRotationRadians.X,
                MathF.PI / 180f * module.RotationDegrees + localRotationRadians.Y,
                localRotationRadians.Z),
            Scale = localScale
        };
    }

    private static Vector3 RotateLocal(Vector3 value, float rotationDegrees)
    {
        var radians = MathF.PI / 180f * rotationDegrees;
        return Vector3.Transform(value, Quaternion.CreateFromAxisAngle(Vector3.UnitY, radians));
    }

    private static Vector4 ToTint(IReadOnlyList<float> color)
    {
        var r = color.Count > 0 ? color[0] : 1f;
        var g = color.Count > 1 ? color[1] : 1f;
        var b = color.Count > 2 ? color[2] : 1f;
        var a = color.Count > 3 ? color[3] : 1f;
        return new Vector4(r, g, b, a);
    }

    private static Vector3 ScalePlanar(Vector3 value, float multiplier)
    {
        return new Vector3(value.X * multiplier, value.Y, value.Z * multiplier);
    }

    private static Vector4 ResolveLightFixtureTint(string archetype)
    {
        return archetype switch
        {
            "safe" => new Vector4(0.92f, 0.88f, 0.70f, 1f),
            "service" => new Vector4(0.74f, 0.66f, 0.40f, 1f),
            "objective" => new Vector4(0.70f, 0.52f, 0.52f, 1f),
            _ => new Vector4(0.76f, 0.78f, 0.82f, 1f)
        };
    }

    private static Vector3 ResolveLightColor(string archetype)
    {
        return archetype switch
        {
            "safe" => new Vector3(1.00f, 0.94f, 0.82f),
            "service" => new Vector3(0.95f, 0.82f, 0.58f),
            "objective" => new Vector3(0.86f, 0.74f, 0.72f),
            "stair" => new Vector3(0.84f, 0.90f, 0.98f),
            _ => new Vector3(0.78f, 0.84f, 0.92f)
        };
    }

    private static float ResolveLightRadius(string archetype, float width, float length)
    {
        var baseRadius = MathF.Max(width, length) * 0.9f;
        return archetype switch
        {
            "safe" => baseRadius + 1.8f,
            "objective" => baseRadius + 1.2f,
            _ => baseRadius
        };
    }

    private static float ResolveLightIntensity(string archetype)
    {
        return archetype switch
        {
            "safe" => 1.18f,
            "service" => 0.94f,
            "objective" => 0.88f,
            _ => 1.00f
        };
    }
}
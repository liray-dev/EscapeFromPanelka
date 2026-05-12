using System.Numerics;
using EFP.Model.Catalog;
using EFP.Model.Common;
using EFP.Model.Raid;
using EFP.Model.Raid.Props;

namespace EFP.Model.WorldGen;

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

        PlaceModules(sector, library, blueprint.Steps, placedByNodeId);

        var safeBlock = sector.Modules.FirstOrDefault(m => IsArchetype(m.Definition, "safe")) ?? sector.Modules[0];
        var objectiveModule = sector.Modules.FirstOrDefault(m => IsArchetype(m.Definition, "objective"))
                              ?? sector.Modules.OrderByDescending(m =>
                                  Vector3.DistanceSquared(m.Position, safeBlock.Position)).First();
        var serviceModule = sector.Modules
                                .Where(m => IsArchetype(m.Definition, "service") && m != safeBlock)
                                .OrderBy(m => Vector3.DistanceSquared(m.Position, safeBlock.Position))
                                .FirstOrDefault()
                            ?? sector.Modules.FirstOrDefault(m => m != safeBlock && m != objectiveModule)
                            ?? safeBlock;

        sector.SafeBlockCenter = safeBlock.Position;
        sector.ExtractionConsolePoint = safeBlock.Position + new Vector3(0f, 0.95f, 0f);
        sector.PowerSwitchPoint = serviceModule.Position + new Vector3(0f, 0.95f, 0f);
        sector.ObjectivePoint = objectiveModule.Position + new Vector3(0f, 0.85f, 0f);
        sector.PlayerSpawn = sector.SafeBlockCenter + new Vector3(0f, 0.5f, 0f);

        var connectedSockets = BuildConnectedSocketSet(blueprint);
        BuildGeometryFor(sector, sector.Modules, connectedSockets);
        BuildConsoles(sector, safeBlock, serviceModule, objectiveModule);
        BuildModuleAdjacency(sector, blueprint.Steps);
        BuildLockablePassagesFor(sector, blueprint.Steps, placedByNodeId, serviceModule, objectiveModule, rng);
        SpawnPropsFor(sector, library, sector.Modules, rng);
        BuildLightsFor(sector, sector.Modules);
        BuildInfectedZonesFor(sector, sector.Modules, rng);
        BuildHostilesFor(sector, sector.Modules, safeBlock, rng);
        BuildLootFor(sector, sector.Modules, objectiveModule, rng);
        ComputeBounds(sector);
        return sector;
    }

    private static void PlaceModules(ProceduralSector sector, ModuleLibrary library,
        IReadOnlyList<BlueprintStep> steps, Dictionary<string, PlacedModule> placedByNodeId)
    {
        foreach (var step in steps)
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
    }

    private static void BuildConsoles(ProceduralSector sector, PlacedModule safeBlock, PlacedModule serviceModule,
        PlacedModule objectiveModule)
    {
        sector.FeatureGeometry.Add(new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = sector.ExtractionConsolePoint - new Vector3(0f, 0.42f, 0f),
                Scale = new Vector3(0.42f, 1.04f, 0.30f)
            },
            new Vector4(0.32f, 0.58f, 0.64f, 1f),
            ownerModuleId: safeBlock.NodeId));

        if (serviceModule != safeBlock)
            sector.FeatureGeometry.Add(new WorldRenderable(
                WorldPrimitiveType.Cube,
                new Transform
                {
                    Position = sector.PowerSwitchPoint - new Vector3(0f, 0.40f, 0f),
                    Scale = new Vector3(0.36f, 1.12f, 0.30f)
                },
                new Vector4(0.64f, 0.52f, 0.18f, 1f),
                ownerModuleId: serviceModule.NodeId));
    }

    private static void BuildModuleAdjacency(ProceduralSector sector, IReadOnlyList<BlueprintStep> steps)
    {
        sector.ModuleAdjacency.Clear();
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.ParentNodeId)) continue;
            var passageId = $"door_{step.NodeId}";
            AppendAdjacency(sector, step.ParentNodeId, step.NodeId, passageId);
            AppendAdjacency(sector, step.NodeId, step.ParentNodeId, passageId);
        }
    }

    private static void AppendAdjacency(ProceduralSector sector, string from, string to, string passageId)
    {
        if (!sector.ModuleAdjacency.TryGetValue(from, out var list))
        {
            list = [];
            sector.ModuleAdjacency[from] = list;
        }

        list.Add(new ModuleAdjacency(to, passageId));
    }

    private static void BuildLockablePassagesFor(ProceduralSector sector, IReadOnlyList<BlueprintStep> steps,
        Dictionary<string, PlacedModule> placedByNodeId, PlacedModule? serviceModule, PlacedModule? objectiveModule,
        Random rng)
    {
        var protectedRouteNodes = serviceModule is not null && objectiveModule is not null
            ? ResolveProtectedRouteNodes(steps, serviceModule.NodeId, objectiveModule.NodeId)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in steps)
        {
            if (string.IsNullOrEmpty(step.ParentNodeId)) continue;
            if (!placedByNodeId.TryGetValue(step.ParentNodeId, out var parent)) continue;
            if (!placedByNodeId.TryGetValue(step.NodeId, out var child)) continue;

            var parentSocket = parent.Definition.Connections
                .First(c => c.Id.Equals(step.ParentSocketId, StringComparison.OrdinalIgnoreCase));
            var worldSocketDirection = parent.GetWorldSocketDirection(step.ParentSocketId);
            var worldSocketPosition = parent.GetWorldSocketPosition(step.ParentSocketId);
            var thickness = parent.Definition.WallThickness;
            var wallHeight = MathF.Max(parent.Definition.WallHeight, child.Definition.WallHeight);

            Vector3 doorScale;
            if (worldSocketDirection is ConnectionDirection.North or ConnectionDirection.South)
                doorScale = new Vector3(parentSocket.OpeningWidth, wallHeight, thickness);
            else
                doorScale = new Vector3(thickness, wallHeight, parentSocket.OpeningWidth);

            var doorPosition = new Vector3(worldSocketPosition.X, wallHeight * 0.5f, worldSocketPosition.Z);
            var doorRenderable = new WorldRenderable(
                WorldPrimitiveType.Cube,
                new Transform { Position = doorPosition, Scale = doorScale },
                new Vector4(0.54f, 0.58f, 0.62f, 1f),
                ownerModuleId: parent.NodeId);

            var leadsToObjective = objectiveModule is not null
                                   && step.NodeId.Equals(objectiveModule.NodeId, StringComparison.OrdinalIgnoreCase);
            var initialState = leadsToObjective ? DoorState.Locked : DoorState.Closed;
            var unlockId = leadsToObjective ? "service_power" : string.Empty;
            var label = leadsToObjective ? "Аварийную переборку к архиву" : "Дверь";
            var jamOnCritical = !leadsToObjective
                                && !protectedRouteNodes.Contains(step.NodeId)
                                && rng.NextDouble() < 0.18;

            if (jamOnCritical)
                doorRenderable = new WorldRenderable(
                    WorldPrimitiveType.Cube,
                    new Transform { Position = doorPosition, Scale = doorScale },
                    new Vector4(0.62f, 0.18f, 0.22f, 1f),
                    ownerModuleId: parent.NodeId);

            var passageId = $"door_{step.NodeId}";
            sector.LockablePassages.Add(new LockablePassage(
                passageId,
                unlockId,
                label,
                leadsToObjective
                    ? new WorldRenderable(WorldPrimitiveType.Cube,
                        new Transform { Position = doorPosition, Scale = doorScale },
                        new Vector4(0.66f, 0.20f, 0.18f, 1f),
                        ownerModuleId: parent.NodeId)
                    : doorRenderable,
                initialState,
                jamOnCritical));
        }
    }

    private static HashSet<string> ResolveProtectedRouteNodes(IReadOnlyList<BlueprintStep> steps, params string[] targets)
    {
        var parentByNode = steps
            .Where(x => !string.IsNullOrWhiteSpace(x.ParentNodeId))
            .ToDictionary(x => x.NodeId, x => x.ParentNodeId!, StringComparer.OrdinalIgnoreCase);

        var protectedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            var current = target;
            while (parentByNode.TryGetValue(current, out var parent))
            {
                protectedNodes.Add(current);
                current = parent;
            }
        }

        return protectedNodes;
    }

    private static HashSet<string> BuildConnectedSocketSet(StructureBlueprint blueprint)
    {
        var connectedSockets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in blueprint.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.ParentNodeId)) continue;

            connectedSockets.Add(SocketKey(step.ParentNodeId, step.ParentSocketId));
            connectedSockets.Add(SocketKey(step.NodeId, step.ChildSocketId));
        }

        return connectedSockets;
    }

    private static string SocketKey(string nodeId, string socketId)
    {
        return $"{nodeId}:{socketId}";
    }

    private static void BuildGeometryFor(ProceduralSector sector, IEnumerable<PlacedModule> modules,
        HashSet<string> connectedSockets)
    {
        foreach (var module in modules)
        {
            AddFloor(sector.StaticGeometry, module);
            AddPerimeterWalls(sector.StaticGeometry, module, connectedSockets);
        }
    }

    private static void BuildLightsFor(ProceduralSector sector, IEnumerable<PlacedModule> modules)
    {
        var baseIndex = sector.Lights.Count;
        var offset = 0;
        foreach (var module in modules)
        {
            var lightLocalPosition = new Vector3(0f, module.Definition.WallHeight - 0.42f, 0f);
            var lightPosition = module.ToWorldPosition(lightLocalPosition);
            var fixtureScale = new Vector3(0.32f, 0.14f, 0.32f);
            var fixtureTint = ResolveLightFixtureTint(module.Definition.Archetype);
            var lightColor = ResolveLightColor(module.Definition.Archetype);
            var radius = ResolveLightRadius(module.Definition.Archetype, module.Definition.Width,
                module.Definition.Length);
            var intensity = ResolveLightIntensity(module.Definition.Archetype);
            var flickerSpeed = module.Definition.Archetype is "service" or "objective" or "basement" ? 4.6f : 0.8f;
            var emergency = !IsArchetype(module.Definition, "safe");

            sector.FeatureGeometry.Add(new WorldRenderable(
                WorldPrimitiveType.Cube,
                new Transform
                {
                    Position = lightPosition,
                    Scale = fixtureScale
                },
                fixtureTint,
                ownerModuleId: module.NodeId));

            sector.Lights.Add(new WorldLight(
                $"light_{module.NodeId}",
                lightPosition,
                lightColor,
                radius,
                intensity,
                flickerSpeed,
                (baseIndex + offset) * 0.71f,
                emergency));

            offset++;
        }
    }

    private static void BuildInfectedZonesFor(ProceduralSector sector, IEnumerable<PlacedModule> modules, Random rng)
    {
        foreach (var module in modules)
        {
            if (IsArchetype(module.Definition, "safe")) continue;
            var chance = module.Definition.InfectedZoneChance;
            if (chance <= 0f || rng.NextDouble() > chance) continue;

            var localOffset = RandomLocalPoint(module.Definition, rng, 0.32f);
            var center = module.ToWorldPosition(new Vector3(localOffset.X, 0.02f, localOffset.Y));
            var minSide = MathF.Min(module.Definition.Width, module.Definition.Length);
            var radius = MathF.Max(0.9f, minSide * (0.30f + (float)rng.NextDouble() * 0.18f));
            var activation = rng.NextDouble() < 0.55 ? RaidPressureLevel.Pressure : RaidPressureLevel.Critical;
            var damage = 6f + (float)rng.NextDouble() * 9f;
            var moveScale = 0.65f + (float)rng.NextDouble() * 0.20f;
            var visibilityBoost = 0.10f + (float)rng.NextDouble() * 0.10f;

            var renderable = new WorldRenderable(
                WorldPrimitiveType.Cube,
                new Transform
                {
                    Position = center + new Vector3(0f, 0.03f, 0f),
                    Scale = new Vector3(radius * 1.8f, 0.06f, radius * 1.6f)
                },
                new Vector4(0.42f, 0.10f, 0.16f, 1f),
                ownerModuleId: module.NodeId);

            sector.InfectedZones.Add(new InfectedZone(
                $"infected_{module.NodeId}",
                $"{ResolveZoneLabel(module.Definition.Archetype)} ({module.Definition.Id})",
                center,
                radius,
                activation,
                moveScale,
                damage,
                visibilityBoost,
                renderable));
        }
    }

    private static void BuildHostilesFor(ProceduralSector sector, IEnumerable<PlacedModule> modules,
        PlacedModule? excludeModule, Random rng)
    {
        foreach (var module in modules)
        {
            if (excludeModule is not null && module == excludeModule) continue;
            var chance = module.Definition.HostileSpawnChance;
            if (chance <= 0f || rng.NextDouble() > chance) continue;

            var pool = HostileCatalog.ForArchetype(module.Definition.Archetype);
            if (pool.Count == 0) pool = HostileCatalog.All;
            if (pool.Count == 0) continue;

            var maxCount = Math.Max(1, module.Definition.MaxHostilesPerModule);
            var count = rng.Next(1, maxCount + 1);
            for (var i = 0; i < count; i++)
            {
                var definition = HostileCatalog.PickWeighted(pool, rng);
                if (definition is null) continue;

                var local = RandomLocalPoint(module.Definition, rng, 0.20f);
                var anchor = module.ToWorldPosition(new Vector3(local.X, 0f, local.Y));
                var size = new Vector3(
                    definition.Size.Length > 0 ? definition.Size[0] : 0.78f,
                    definition.Size.Length > 1 ? definition.Size[1] : 1.16f,
                    definition.Size.Length > 2 ? definition.Size[2] : 0.78f);

                sector.Hostiles.Add(new Hostile(
                    $"hostile_{module.NodeId}_{i}",
                    definition.Label,
                    anchor,
                    ToTint(definition.DormantTint),
                    ToTint(definition.AlertTint),
                    definition.MaxHealth,
                    definition.CollisionRadius,
                    size,
                    definition.ModelId,
                    module.NodeId));
            }
        }
    }

    private static void BuildLootFor(ProceduralSector sector, IEnumerable<PlacedModule> modules,
        PlacedModule? forceSpawnModule, Random rng)
    {
        foreach (var module in modules)
        {
            var pool = LootCatalog.ForArchetype(module.Definition.Archetype);
            if (pool.Count == 0) pool = LootCatalog.All;
            if (pool.Count == 0) continue;

            var spawnRoll = rng.NextDouble();
            var forceSpawn = forceSpawnModule is not null && module == forceSpawnModule;
            if (!forceSpawn && spawnRoll > module.Definition.LootSpawnChance) continue;

            var maxCount = Math.Max(1, module.Definition.MaxLootPerModule);
            var count = rng.Next(1, maxCount + 1);
            if (forceSpawn) count = Math.Max(count, 2);

            for (var i = 0; i < count; i++)
            {
                var definition = LootCatalog.PickWeighted(pool, rng);
                if (definition is null) continue;

                var local = RandomLocalPoint(module.Definition, rng, 0.18f);
                var size = new Vector3(
                    definition.Size.Length > 0 ? definition.Size[0] : 0.26f,
                    definition.Size.Length > 1 ? definition.Size[1] : 0.26f,
                    definition.Size.Length > 2 ? definition.Size[2] : 0.26f);
                var localPosition = new Vector3(local.X, size.Y * 0.5f + 0.06f, local.Y);
                var transform = CreateWorldTransform(module, localPosition, Vector3.Zero, size);
                var tint = ToTint(definition.Color);
                var renderable = new WorldRenderable(WorldPrimitiveType.Cube, transform, tint, definition.ModelId,
                    ownerModuleId: module.NodeId);

                var pickup = new LootPickup(
                    $"loot_{module.NodeId}_{i}_{definition.Id}",
                    definition.Label,
                    definition.Kind,
                    transform.Position,
                    renderable,
                    definition.Value,
                    definition.MedkitCount,
                    definition.ModelId);
                sector.Loot.Add(pickup);
                sector.Containers.Add(new Container(pickup, ResolveSearchDuration(definition.Kind)));
            }
        }
    }

    private static float ResolveSearchDuration(LootKind kind)
    {
        return kind switch
        {
            LootKind.Medkit => 1.10f,
            LootKind.Filter => 1.20f,
            LootKind.Battery => 1.30f,
            LootKind.Scrap => 1.00f,
            LootKind.Reagent => 1.50f,
            _ => 1.20f
        };
    }

    private static Vector2 RandomLocalPoint(ModuleDefinition definition, Random rng, float marginScale)
    {
        var marginX = MathF.Max(definition.WallThickness + 0.4f, definition.Width * marginScale);
        var marginZ = MathF.Max(definition.WallThickness + 0.4f, definition.Length * marginScale);
        var halfX = MathF.Max(0.05f, definition.Width * 0.5f - marginX);
        var halfZ = MathF.Max(0.05f, definition.Length * 0.5f - marginZ);
        var x = ((float)rng.NextDouble() * 2f - 1f) * halfX;
        var z = ((float)rng.NextDouble() * 2f - 1f) * halfZ;
        return new Vector2(x, z);
    }

    private static string ResolveZoneLabel(string archetype)
    {
        return archetype switch
        {
            "corridor" => "Коридор затягивает слизью",
            "service" => "Служебная отсек заражён",
            "objective" => "Архивная комната заражена",
            "stair" => "Лестничный марш режет туманом",
            "residential" => "Квартира поражена самосбором",
            "basement" => "Подвал кишит самосбором",
            _ => "Сектор поражён"
        };
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

        geometry.Add(new WorldRenderable(WorldPrimitiveType.Cube, floorTransform, floorColor,
            ownerModuleId: module.NodeId));
    }

    private static void AddPerimeterWalls(List<WorldRenderable> geometry, PlacedModule module,
        HashSet<string> connectedSockets)
    {
        AddEdgeWalls(geometry, module, ConnectionDirection.North, connectedSockets);
        AddEdgeWalls(geometry, module, ConnectionDirection.South, connectedSockets);
        AddEdgeWalls(geometry, module, ConnectionDirection.East, connectedSockets);
        AddEdgeWalls(geometry, module, ConnectionDirection.West, connectedSockets);
    }

    private static void AddEdgeWalls(List<WorldRenderable> geometry, PlacedModule module, ConnectionDirection direction,
        HashSet<string> connectedSockets)
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
            .Where(x => x.Direction == direction && connectedSockets.Contains(SocketKey(module.NodeId, x.Id)))
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
            CreateWorldTransform(module, localPosition, Vector3.Zero, localScale), tint,
            ownerModuleId: module.NodeId));
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
            CreateWorldTransform(module, localPosition, Vector3.Zero, localScale), tint,
            ownerModuleId: module.NodeId));
    }

    private static void SpawnPropsFor(ProceduralSector sector, ModuleLibrary library,
        IEnumerable<PlacedModule> modules, Random rng)
    {
        foreach (var module in modules)
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
            var renderable = new WorldRenderable(WorldPrimitiveType.Cube, transform, tint,
                ownerModuleId: module.NodeId);

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

    private static bool IsArchetype(ModuleDefinition module, string archetype)
    {
        return module.Archetype.Equals(archetype, StringComparison.OrdinalIgnoreCase);
    }

    private static Vector4 ResolveLightFixtureTint(string archetype)
    {
        return archetype switch
        {
            "safe" => new Vector4(0.92f, 0.88f, 0.70f, 1f),
            "service" => new Vector4(0.74f, 0.66f, 0.40f, 1f),
            "objective" => new Vector4(0.70f, 0.52f, 0.52f, 1f),
            "basement" => new Vector4(0.42f, 0.46f, 0.50f, 1f),
            "residential" => new Vector4(0.82f, 0.74f, 0.58f, 1f),
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
            "basement" => new Vector3(0.62f, 0.74f, 0.84f),
            "residential" => new Vector3(0.94f, 0.86f, 0.74f),
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
            "basement" => 0.78f,
            "residential" => 1.04f,
            _ => 1.00f
        };
    }
}

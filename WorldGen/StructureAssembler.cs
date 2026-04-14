using System.Numerics;
using EFP.Entities;
using EFP.World;

namespace EFP.WorldGen;

public sealed class StructureAssembler
{
    public ProceduralSector Assemble(StructureBlueprint blueprint, ModuleLibrary library)
    {
        var sector = new ProceduralSector { Seed = blueprint.Seed };
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
                var parentSocket = parent.GetSocket(step.ParentSocketId);
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

        sector.SafeBlockCenter = safeBlock.Position;
        sector.ExtractionConsolePoint = safeBlock.ToWorldPosition(new Vector3(2.15f, 0.95f, -1.25f));
        sector.PowerSwitchPoint = serviceNook.ToWorldPosition(new Vector3(2.05f, 0.95f, -1.10f));
        sector.ObjectivePoint = objectiveRoom.Position + new Vector3(0f, 0.85f, 0f);
        sector.PlayerSpawn = sector.SafeBlockCenter + new Vector3(0f, 0.5f, 0f);

        BuildGeometry(sector);
        BuildFeatures(sector, safeBlock, hallA, serviceNook, hallB, objectiveRoom);
        SpawnProps(sector, library, rng);
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
        PlacedModule serviceNook, PlacedModule hallB, PlacedModule objectiveRoom)
    {
        var extractionConsole = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                safeBlock,
                new Vector3(2.15f, 0.52f, -1.25f),
                Vector3.Zero,
                new Vector3(0.38f, 1.04f, 0.24f)),
            new Vector4(0.32f, 0.58f, 0.64f, 1f));

        var powerConsole = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                serviceNook,
                new Vector3(2.05f, 0.56f, -1.10f),
                Vector3.Zero,
                new Vector3(0.36f, 1.12f, 0.24f)),
            new Vector4(0.64f, 0.52f, 0.18f, 1f));

        sector.FeatureGeometry.Add(extractionConsole);
        sector.FeatureGeometry.Add(powerConsole);

        var serviceDoor = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                hallA,
                new Vector3(0.8f, 1.12f, -hallA.Definition.Length * 0.5f + hallA.Definition.WallThickness * 0.5f),
                Vector3.Zero,
                new Vector3(1.82f, 2.24f, 0.18f)),
            new Vector4(0.54f, 0.58f, 0.62f, 1f));

        var archiveBulkhead = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                hallB,
                new Vector3(hallB.Definition.Width * 0.5f - hallB.Definition.WallThickness * 0.5f, 1.15f, 0f),
                Vector3.Zero,
                new Vector3(0.32f, 2.30f, 1.92f)),
            new Vector4(0.66f, 0.20f, 0.18f, 1f));

        var quarantineShutter = new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                hallA,
                new Vector3(0.8f, 1.15f,
                    -hallA.Definition.Length * 0.5f + hallA.Definition.WallThickness * 0.5f - 0.24f),
                Vector3.Zero,
                new Vector3(2.10f, 2.30f, 0.28f)),
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
                new Vector3(0.4f, 0.62f, -hallA.Definition.Length * 0.5f + 1.05f),
                Vector3.Zero,
                new Vector3(2.85f, 1.22f, 1.45f)),
            new Vector4(0.42f, 0.11f, 0.22f, 1f)));

        sector.CriticalMutationGeometry.Add(new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                hallA,
                new Vector3(1.35f, 1.66f, -hallA.Definition.Length * 0.5f + 1.15f),
                Vector3.Zero,
                new Vector3(1.10f, 0.92f, 0.86f)),
            new Vector4(0.54f, 0.14f, 0.25f, 1f)));

        sector.CriticalMutationGeometry.Add(new WorldRenderable(
            WorldPrimitiveType.Cube,
            CreateWorldTransform(
                objectiveRoom,
                new Vector3(2.4f, 0.72f, -2.1f),
                Vector3.Zero,
                new Vector3(1.28f, 1.44f, 1.05f)),
            new Vector4(0.36f, 0.10f, 0.20f, 1f)));
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
            Rotation = localRotationRadians with { Y = MathF.PI / 180f * module.RotationDegrees + localRotationRadians.Y },
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
}
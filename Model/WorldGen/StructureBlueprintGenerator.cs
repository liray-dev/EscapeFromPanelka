using System.Numerics;

namespace EFP.Model.WorldGen;

public sealed class StructureBlueprintGenerator
{
    private const int MaxAttemptsPerSocket = 48;
    private const float OverlapEpsilon = 0.06f;
    private const int MaxBlueprintAttempts = 18;

    public static StructureBlueprint Generate(int seed, ModuleLibrary library, int minModules, int maxModules)
    {
        return GenerateWithState(seed, library, minModules, maxModules).Blueprint;
    }

    public static SectorBuildState GenerateWithState(int seed, ModuleLibrary library, int minModules, int maxModules)
    {
        var clampedMin = Math.Max(2, minModules);
        var best = GenerateOnce(seed, library, clampedMin, maxModules);
        if (IsValid(best.Blueprint, library, clampedMin)) return best;

        for (var attempt = 1; attempt < MaxBlueprintAttempts; attempt++)
        {
            var candidateSeed = unchecked(seed + attempt * 7919);
            var candidate = GenerateOnce(candidateSeed, library, clampedMin, maxModules);
            if (candidate.Blueprint.Steps.Count > best.Blueprint.Steps.Count) best = candidate;
            if (IsValid(candidate.Blueprint, library, clampedMin))
            {
                candidate.Blueprint.Seed = seed;
                return candidate;
            }
        }

        var fallback = GenerateFallback(seed, library, clampedMin);
        best.Blueprint.Seed = seed;
        return IsValid(fallback.Blueprint, library, clampedMin) ? fallback : best;
    }

    public static List<BlueprintStep> Grow(SectorBuildState state, ModuleLibrary library, Random rng,
        Vector3 anchorPosition, float maxAnchorDistance, int budget)
    {
        var added = new List<BlueprintStep>();
        if (state.OpenSockets.Count == 0 || budget <= 0) return added;

        var anchorXZ = new Vector2(anchorPosition.X, anchorPosition.Z);
        var candidates = state.OpenSockets
            .Select(socket =>
            {
                var worldPosition = ResolveSocketWorldPosition(state.PlacedByNodeId, socket);
                var distance = Vector2.Distance(anchorXZ, new Vector2(worldPosition.X, worldPosition.Z));
                return (Socket: socket, Distance: distance);
            })
            .Where(x => x.Distance <= maxAnchorDistance)
            .OrderBy(x => x.Distance)
            .Take(budget * 3)
            .ToList();

        if (candidates.Count == 0) return added;

        var growthPool = library.AllModules
            .Where(m => !IsArchetype(m, "objective") && !IsArchetype(m, "safe") && m.Connections.Count > 0)
            .ToList();
        if (growthPool.Count == 0) return added;

        foreach (var (socket, _) in candidates)
        {
            if (added.Count >= budget) break;
            if (!state.OpenSockets.Remove(socket)) continue;

            if (!TryAttach(socket, growthPool, state.PlacedByNodeId, rng, out var step, out var placement)) continue;

            var nodeId = $"module_{state.NextNodeIndex++}";
            step!.NodeId = nodeId;
            state.Blueprint.Steps.Add(step);
            state.PlacedByNodeId[nodeId] = placement;
            added.Add(step);

            foreach (var connection in placement.Definition.Connections)
                if (!connection.Id.Equals(step.ChildSocketId, StringComparison.OrdinalIgnoreCase))
                    state.OpenSockets.Add(new OpenSocket(nodeId, connection.Id));
        }

        return added;
    }

    private static SectorBuildState GenerateOnce(int seed, ModuleLibrary library, int minModules, int maxModules)
    {
        var rng = new Random(seed);
        var state = new SectorBuildState
        {
            Blueprint = new StructureBlueprint { Seed = seed }
        };

        var clampedMin = Math.Max(2, minModules);
        var clampedMax = Math.Max(clampedMin, maxModules);
        var targetCount = clampedMin == clampedMax ? clampedMin : rng.Next(clampedMin, clampedMax + 1);

        var allModules = library.AllModules;
        if (allModules.Count == 0) return state;

        var safeCandidates = allModules
            .Where(m => IsArchetype(m, "safe") && m.Connections.Count > 0)
            .ToList();
        var rootDef = safeCandidates.Count > 0
            ? safeCandidates[rng.Next(safeCandidates.Count)]
            : allModules.First(m => m.Connections.Count > 0);

        var rootNodeId = "module_0";
        state.PlacedByNodeId[rootNodeId] = new Placement(rootDef, Vector3.Zero, 0f);
        state.Blueprint.Steps.Add(new BlueprintStep { NodeId = rootNodeId, ModuleId = rootDef.Id, MainRoute = true });

        foreach (var connection in rootDef.Connections)
            state.OpenSockets.Add(new OpenSocket(rootNodeId, connection.Id));

        var hasService = IsArchetype(rootDef, "service");
        var hasObjective = IsArchetype(rootDef, "objective");
        state.NextNodeIndex = 1;
        var iterations = 0;
        var safetyLimit = Math.Max(600, targetCount * 96);

        while (state.PlacedByNodeId.Count < targetCount && state.OpenSockets.Count > 0 && iterations++ < safetyLimit)
        {
            var socketIndex = rng.Next(state.OpenSockets.Count);
            var open = state.OpenSockets[socketIndex];
            state.OpenSockets.RemoveAt(socketIndex);

            var requireService = !hasService && state.PlacedByNodeId.Count >= targetCount - 3;
            var requireObjective = !hasObjective && state.PlacedByNodeId.Count >= targetCount - 1;
            var remainingModules = targetCount - state.PlacedByNodeId.Count;
            var needsMoreFrontier = state.OpenSockets.Count <= remainingModules;
            var candidatePool = CreateCandidatePool(allModules, requireService, requireObjective, needsMoreFrontier);
            if (candidatePool.Count == 0) continue;

            if (TryAttach(open, candidatePool, state.PlacedByNodeId, rng, out var step, out var placement))
            {
                var nodeId = $"module_{state.NextNodeIndex++}";
                step!.NodeId = nodeId;
                state.Blueprint.Steps.Add(step);
                state.PlacedByNodeId[nodeId] = placement;

                if (!IsArchetype(placement.Definition, "objective"))
                    foreach (var connection in placement.Definition.Connections)
                        if (!connection.Id.Equals(step.ChildSocketId, StringComparison.OrdinalIgnoreCase))
                            state.OpenSockets.Add(new OpenSocket(nodeId, connection.Id));

                if (IsArchetype(placement.Definition, "service")) hasService = true;
                if (IsArchetype(placement.Definition, "objective")) hasObjective = true;
            }
        }

        EnsureArchetypePresent(state, library, rng, "service", ref hasService);
        EnsureArchetypePresent(state, library, rng, "objective", ref hasObjective);

        return state;
    }

    private static void EnsureArchetypePresent(SectorBuildState state, ModuleLibrary library, Random rng,
        string archetype, ref bool flag)
    {
        if (flag || state.OpenSockets.Count == 0) return;

        var pool = library.AllModules.Where(m => IsArchetype(m, archetype) && m.Connections.Count > 0).ToList();
        if (pool.Count == 0) return;

        for (var i = state.OpenSockets.Count - 1; i >= 0 && !flag; i--)
        {
            var open = state.OpenSockets[i];
            if (!TryAttach(open, pool, state.PlacedByNodeId, rng, out var step, out var placement)) continue;

            state.OpenSockets.RemoveAt(i);
            var nodeId = $"module_{state.NextNodeIndex++}";
            step!.NodeId = nodeId;
            if (archetype == "objective") step.MainRoute = true;
            state.Blueprint.Steps.Add(step);
            state.PlacedByNodeId[nodeId] = placement;

            if (archetype != "objective")
                foreach (var connection in placement.Definition.Connections)
                    if (!connection.Id.Equals(step.ChildSocketId, StringComparison.OrdinalIgnoreCase))
                        state.OpenSockets.Add(new OpenSocket(nodeId, connection.Id));

            flag = true;
        }
    }

    private static List<ModuleDefinition> CreateCandidatePool(IReadOnlyList<ModuleDefinition> allModules,
        bool requireService, bool requireObjective, bool needsMoreFrontier)
    {
        if (requireObjective)
            return allModules.Where(m => IsArchetype(m, "objective") && m.Connections.Count > 0).ToList();

        if (requireService)
        {
            var services = allModules.Where(m => IsArchetype(m, "service") && m.Connections.Count > 0).ToList();
            if (services.Count > 0) return services;
        }

        var nonObjective = allModules
            .Where(m => !IsArchetype(m, "objective") && m.Connections.Count > 0)
            .ToList();

        if (!needsMoreFrontier) return nonObjective;

        var expanding = nonObjective.Where(m => m.Connections.Count > 1).ToList();
        return expanding.Count > 0 ? expanding : nonObjective;
    }

    private static bool TryAttach(OpenSocket open, List<ModuleDefinition> candidatePool,
        Dictionary<string, Placement> placed, Random rng, out BlueprintStep? step, out Placement placement)
    {
        step = null;
        placement = default;

        var parent = placed[open.NodeId];
        var parentSocket = parent.Definition.Connections
            .First(c => c.Id.Equals(open.SocketId, StringComparison.OrdinalIgnoreCase));
        var parentSocketWorldDirection = WorldGenMath.RotateDirection(parentSocket.Direction, parent.RotationDegrees);
        var parentSocketLocal = WorldGenMath.GetSocketLocalPosition(parent.Definition, parentSocket);
        var parentSocketWorldPosition = parent.Position + RotateLocal(parentSocketLocal, parent.RotationDegrees);
        var desiredChildDirection = WorldGenMath.Opposite(parentSocketWorldDirection);

        var attempts = Math.Min(MaxAttemptsPerSocket, candidatePool.Count * 4 + 4);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var candidate = PickWeighted(candidatePool, rng);
            if (candidate.Connections.Count == 0) continue;

            var sockets = candidate.Connections.ToList();
            Shuffle(sockets, rng);

            foreach (var childSocket in sockets)
            {
                var rotation = WorldGenMath.SolveRotationDegrees(childSocket.Direction, desiredChildDirection);
                var rotatedChildLocal = RotateLocal(
                    WorldGenMath.GetSocketLocalPosition(candidate, childSocket), rotation);
                var childPosition = parentSocketWorldPosition - rotatedChildLocal;

                if (Overlaps(candidate, childPosition, rotation, placed, parent)) continue;

                placement = new Placement(candidate, childPosition, rotation);
                step = new BlueprintStep
                {
                    ModuleId = candidate.Id,
                    ParentNodeId = open.NodeId,
                    ParentSocketId = open.SocketId,
                    ChildSocketId = childSocket.Id,
                    MainRoute = rng.NextDouble() < 0.5
                };
                return true;
            }
        }

        return false;
    }

    private static Vector3 ResolveSocketWorldPosition(Dictionary<string, Placement> placed, OpenSocket socket)
    {
        if (!placed.TryGetValue(socket.NodeId, out var parent)) return Vector3.Zero;
        var connection = parent.Definition.Connections
            .FirstOrDefault(c => c.Id.Equals(socket.SocketId, StringComparison.OrdinalIgnoreCase));
        if (connection is null) return parent.Position;

        var local = WorldGenMath.GetSocketLocalPosition(parent.Definition, connection);
        return parent.Position + RotateLocal(local, parent.RotationDegrees);
    }

    private static bool Overlaps(ModuleDefinition definition, Vector3 position, float rotationDegrees,
        Dictionary<string, Placement> placed, Placement excludeParent)
    {
        var halfA = GetPlanarHalfExtents(definition, rotationDegrees);
        var minA = new Vector2(position.X - halfA.X + OverlapEpsilon, position.Z - halfA.Y + OverlapEpsilon);
        var maxA = new Vector2(position.X + halfA.X - OverlapEpsilon, position.Z + halfA.Y - OverlapEpsilon);

        foreach (var other in placed.Values)
        {
            if (ReferenceEquals(other.Definition, excludeParent.Definition)
                && other.Position == excludeParent.Position
                && other.RotationDegrees == excludeParent.RotationDegrees) continue;

            var halfB = GetPlanarHalfExtents(other.Definition, other.RotationDegrees);
            var minB = new Vector2(other.Position.X - halfB.X, other.Position.Z - halfB.Y);
            var maxB = new Vector2(other.Position.X + halfB.X, other.Position.Z + halfB.Y);

            if (maxA.X < minB.X || minA.X > maxB.X) continue;
            if (maxA.Y < minB.Y || minA.Y > maxB.Y) continue;
            return true;
        }

        return false;
    }

    private static Vector2 GetPlanarHalfExtents(ModuleDefinition definition, float rotationDegrees)
    {
        var halfX = definition.Width * 0.5f;
        var halfZ = definition.Length * 0.5f;
        var quarterTurns = (int)MathF.Round(rotationDegrees / 90f);
        var normalized = (quarterTurns % 4 + 4) % 4;
        return normalized % 2 == 0 ? new Vector2(halfX, halfZ) : new Vector2(halfZ, halfX);
    }

    private static Vector3 RotateLocal(Vector3 value, float rotationDegrees)
    {
        var radians = MathF.PI / 180f * rotationDegrees;
        return Vector3.Transform(value, Quaternion.CreateFromAxisAngle(Vector3.UnitY, radians));
    }

    private static ModuleDefinition PickWeighted(List<ModuleDefinition> pool, Random rng)
    {
        var total = 0;
        foreach (var p in pool) total += Math.Max(1, p.Weight);
        var roll = rng.Next(total);
        var acc = 0;
        foreach (var p in pool)
        {
            acc += Math.Max(1, p.Weight);
            if (roll < acc) return p;
        }

        return pool[^1];
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static bool IsArchetype(ModuleDefinition module, string archetype)
    {
        return module.Archetype.Equals(archetype, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValid(StructureBlueprint blueprint, ModuleLibrary library, int minModules)
    {
        if (blueprint.Steps.Count < minModules) return false;

        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasSafe = false;
        var hasService = false;
        var hasObjective = false;
        var childrenByParent = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? rootNodeId = null;

        foreach (var step in blueprint.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.NodeId) || !nodeIds.Add(step.NodeId)) return false;

            var module = library.GetModule(step.ModuleId);
            hasSafe |= IsArchetype(module, "safe");
            hasService |= IsArchetype(module, "service");
            hasObjective |= IsArchetype(module, "objective");

            if (string.IsNullOrWhiteSpace(step.ParentNodeId))
            {
                if (rootNodeId is not null) return false;
                rootNodeId = step.NodeId;
            }
            else
            {
                if (!nodeIds.Contains(step.ParentNodeId)) return false;
                if (!childrenByParent.TryGetValue(step.ParentNodeId, out var children))
                {
                    children = [];
                    childrenByParent[step.ParentNodeId] = children;
                }

                children.Add(step.NodeId);
            }
        }

        return rootNodeId is not null
               && hasSafe
               && hasService
               && hasObjective
               && CountReachableNodes(rootNodeId, childrenByParent) == blueprint.Steps.Count;
    }

    private static SectorBuildState GenerateFallback(int seed, ModuleLibrary library, int minModules)
    {
        var state = new SectorBuildState
        {
            Blueprint = new StructureBlueprint { Seed = seed }
        };
        var safe = FindModule(library, "safe", ConnectionDirection.East);
        var corridor = FindPassThroughModule(library, "corridor", ConnectionDirection.West, ConnectionDirection.East);
        var service = FindPassThroughModule(library, "service", ConnectionDirection.West, ConnectionDirection.East);
        var objective = FindModule(library, "objective", ConnectionDirection.West);

        if (safe is null || corridor is null || service is null || objective is null) return state;

        var rootNodeId = "module_0";
        state.Blueprint.Steps.Add(new BlueprintStep { NodeId = rootNodeId, ModuleId = safe.Id, MainRoute = true });
        state.PlacedByNodeId[rootNodeId] = new Placement(safe, Vector3.Zero, 0f);

        var parentNodeId = rootNodeId;
        var parentSocketId = safe.Connections.First(x => x.Direction == ConnectionDirection.East).Id;
        state.NextNodeIndex = 1;
        var corridorCount = Math.Max(1, minModules - 3);

        for (var i = 0; i < corridorCount; i++)
        {
            var childNodeId = $"module_{state.NextNodeIndex++}";
            state.Blueprint.Steps.Add(new BlueprintStep
            {
                NodeId = childNodeId,
                ModuleId = corridor.Id,
                ParentNodeId = parentNodeId,
                ParentSocketId = parentSocketId,
                ChildSocketId = corridor.Connections.First(x => x.Direction == ConnectionDirection.West).Id,
                MainRoute = true
            });

            parentNodeId = childNodeId;
            parentSocketId = corridor.Connections.First(x => x.Direction == ConnectionDirection.East).Id;
        }

        var serviceNodeId = $"module_{state.NextNodeIndex++}";
        state.Blueprint.Steps.Add(new BlueprintStep
        {
            NodeId = serviceNodeId,
            ModuleId = service.Id,
            ParentNodeId = parentNodeId,
            ParentSocketId = parentSocketId,
            ChildSocketId = service.Connections.First(x => x.Direction == ConnectionDirection.West).Id,
            MainRoute = true
        });

        parentNodeId = serviceNodeId;
        parentSocketId = service.Connections.First(x => x.Direction == ConnectionDirection.East).Id;

        state.Blueprint.Steps.Add(new BlueprintStep
        {
            NodeId = $"module_{state.NextNodeIndex++}",
            ModuleId = objective.Id,
            ParentNodeId = parentNodeId,
            ParentSocketId = parentSocketId,
            ChildSocketId = objective.Connections.First(x => x.Direction == ConnectionDirection.West).Id,
            MainRoute = true
        });

        return state;
    }

    private static ModuleDefinition? FindPassThroughModule(ModuleLibrary library, string archetype,
        ConnectionDirection entry, ConnectionDirection exit)
    {
        return library.AllModules.FirstOrDefault(m =>
            IsArchetype(m, archetype)
            && m.Connections.Any(x => x.Direction == entry)
            && m.Connections.Any(x => x.Direction == exit));
    }

    private static ModuleDefinition? FindModule(ModuleLibrary library, string archetype, ConnectionDirection socket)
    {
        return library.AllModules.FirstOrDefault(m =>
            IsArchetype(m, archetype) && m.Connections.Any(x => x.Direction == socket));
    }

    private static int CountReachableNodes(string rootNodeId, Dictionary<string, List<string>> childrenByParent)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(rootNodeId);

        while (queue.Count > 0)
        {
            var nodeId = queue.Dequeue();
            if (!visited.Add(nodeId)) continue;
            if (!childrenByParent.TryGetValue(nodeId, out var children)) continue;

            foreach (var child in children)
                queue.Enqueue(child);
        }

        return visited.Count;
    }
}

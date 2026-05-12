using System.Numerics;

namespace EFP.Model.WorldGen;

public sealed class StructureBlueprintGenerator
{
    private const int MaxAttemptsPerSocket = 6;
    private const int SocketRetryBudget = 3;
    private const int MaxBlueprintAttempts = 24;

    public static StructureBlueprint Generate(int seed, ModuleLibrary library, int minModules, int maxModules)
    {
        var clampedMin = Math.Max(2, minModules);
        var best = GenerateOnce(seed, library, clampedMin, maxModules);
        if (Validate(best.Blueprint, library, clampedMin)) return best.Blueprint;

        for (var attempt = 1; attempt < MaxBlueprintAttempts; attempt++)
        {
            var candidateSeed = unchecked(seed + attempt * 7919);
            var candidate = GenerateOnce(candidateSeed, library, clampedMin, maxModules);
            if (!Validate(candidate.Blueprint, library, clampedMin)) continue;

            candidate.Blueprint.Seed = seed;
            return candidate.Blueprint;
        }

        var fallback = GenerateFallback(seed, library, clampedMin);
        fallback.Blueprint.Seed = seed;
        return Validate(fallback.Blueprint, library, clampedMin) ? fallback.Blueprint : best.Blueprint;
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

        var attemptsBySocket = new Dictionary<OpenSocket, int>();
        var iterations = 0;
        var safetyLimit = Math.Max(800, targetCount * 64);

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
                attemptsBySocket.Remove(open);
            }
            else
            {
                var tries = attemptsBySocket.TryGetValue(open, out var current) ? current + 1 : 1;
                if (tries < SocketRetryBudget)
                {
                    attemptsBySocket[open] = tries;
                    state.OpenSockets.Add(open);
                }
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

        for (var attempt = 0; attempt < MaxAttemptsPerSocket; attempt++)
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

                if (Overlaps(candidate, childPosition, rotation, placed)) continue;

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

    private static bool Overlaps(ModuleDefinition definition, Vector3 position, float rotationDegrees,
        Dictionary<string, Placement> placed)
    {
        var halfA = GetPlanarHalfExtents(definition, rotationDegrees);
        var minA = new Vector2(position.X - halfA.X, position.Z - halfA.Y);
        var maxA = new Vector2(position.X + halfA.X, position.Z + halfA.Y);

        foreach (var other in placed.Values)
        {
            var halfB = GetPlanarHalfExtents(other.Definition, other.RotationDegrees);
            var minB = new Vector2(other.Position.X - halfB.X, other.Position.Z - halfB.Y);
            var maxB = new Vector2(other.Position.X + halfB.X, other.Position.Z + halfB.Y);

            const float Tolerance = 0.01f;
            if (maxA.X <= minB.X + Tolerance || minA.X >= maxB.X - Tolerance) continue;
            if (maxA.Y <= minB.Y + Tolerance || minA.Y >= maxB.Y - Tolerance) continue;
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

    private static bool Validate(StructureBlueprint blueprint, ModuleLibrary library, int minModules)
    {
        if (blueprint.Steps.Count < minModules) return false;

        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var childrenByParent = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? rootNodeId = null;
        string? serviceNodeId = null;
        string? objectiveNodeId = null;

        foreach (var step in blueprint.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.NodeId) || !nodeIds.Add(step.NodeId)) return false;

            var module = library.GetModule(step.ModuleId);
            if (IsArchetype(module, "service") && serviceNodeId is null) serviceNodeId = step.NodeId;
            if (IsArchetype(module, "objective") && objectiveNodeId is null) objectiveNodeId = step.NodeId;

            if (string.IsNullOrWhiteSpace(step.ParentNodeId))
            {
                if (rootNodeId is not null) return false;
                if (!IsArchetype(module, "safe")) return false;
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

        if (rootNodeId is null || serviceNodeId is null || objectiveNodeId is null) return false;

        var reachable = ReachableNodes(rootNodeId, childrenByParent);
        if (reachable.Count != blueprint.Steps.Count) return false;
        if (!reachable.Contains(serviceNodeId)) return false;
        if (!reachable.Contains(objectiveNodeId)) return false;

        return true;
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

    private static HashSet<string> ReachableNodes(string rootNodeId, Dictionary<string, List<string>> childrenByParent)
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

        return visited;
    }
}

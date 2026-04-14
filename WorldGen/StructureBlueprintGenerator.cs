namespace EFP.WorldGen;

public sealed class StructureBlueprintGenerator
{
    public StructureBlueprint Generate(int seed)
    {
        return new StructureBlueprint
        {
            Seed = seed,
            Steps =
            [
                new BlueprintStep
                {
                    NodeId = "safe_block",
                    ModuleId = "safe_room"
                },
                new BlueprintStep
                {
                    NodeId = "hall_a",
                    ModuleId = "corridor_long",
                    ParentNodeId = "safe_block",
                    ParentSocketId = "east",
                    ChildSocketId = "west",
                    MainRoute = true
                },
                new BlueprintStep
                {
                    NodeId = "service_nook",
                    ModuleId = "service_nook",
                    ParentNodeId = "hall_a",
                    ParentSocketId = "north",
                    ChildSocketId = "south",
                    MainRoute = false
                },
                new BlueprintStep
                {
                    NodeId = "stair_hall",
                    ModuleId = "stair_hall",
                    ParentNodeId = "hall_a",
                    ParentSocketId = "east",
                    ChildSocketId = "west",
                    MainRoute = true
                },
                new BlueprintStep
                {
                    NodeId = "hall_b",
                    ModuleId = "corridor_short",
                    ParentNodeId = "stair_hall",
                    ParentSocketId = "south",
                    ChildSocketId = "north",
                    MainRoute = true
                },
                new BlueprintStep
                {
                    NodeId = "objective_room",
                    ModuleId = "objective_room",
                    ParentNodeId = "hall_b",
                    ParentSocketId = "east",
                    ChildSocketId = "west",
                    MainRoute = true
                }
            ]
        };
    }
}

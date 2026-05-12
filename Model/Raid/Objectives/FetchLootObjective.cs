namespace EFP.Model.Raid.Objectives;

public sealed class FetchLootObjective : RaidObjective
{
    public FetchLootObjective(LootKind kind, int count, string label)
    {
        Kind = kind;
        Target = Math.Max(1, count);
        Label = label;
    }

    public LootKind Kind { get; }
    public int Target { get; }
    public string Label { get; }

    public override RaidPhase Phase => RaidPhase.ReturnToSafeBlock;

    public override string Describe(Raid raid)
    {
        return $"Принести {Label} ×{Target} ({Math.Min(Target, CollectedCount(raid))}/{Target})";
    }

    public override bool IsComplete(Raid raid) => CollectedCount(raid) >= Target;

    private int CollectedCount(Raid raid)
    {
        var count = 0;
        foreach (var pickup in raid.Sector.Loot)
            if (pickup.Collected && pickup.Kind == Kind) count++;
        return count;
    }
}

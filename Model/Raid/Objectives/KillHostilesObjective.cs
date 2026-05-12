namespace EFP.Model.Raid.Objectives;

public sealed class KillHostilesObjective : RaidObjective
{
    public KillHostilesObjective(int target)
    {
        Target = Math.Max(1, target);
    }

    public int Target { get; }

    public override RaidPhase Phase => RaidPhase.ReturnToSafeBlock;

    public override string Describe(Raid raid)
    {
        return $"Уложить {Target} нежетей ({Math.Min(Target, raid.HostileKillCount)}/{Target})";
    }

    public override bool IsComplete(Raid raid) => raid.HostileKillCount >= Target;
}

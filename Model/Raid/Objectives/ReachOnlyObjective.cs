namespace EFP.Model.Raid.Objectives;

public sealed class ReachOnlyObjective : RaidObjective
{
    public override RaidPhase Phase => RaidPhase.ReturnToSafeBlock;
    public override string Describe(Raid raid) => "Просто добраться до точки выхода";
    public override bool IsComplete(Raid raid) => true;
}

namespace EFP.Model.Raid.Objectives;

public sealed class ReachExtractionObjective : RaidObjective
{
    public override RaidPhase Phase => RaidPhase.ReturnToSafeBlock;
    public override string Describe(Raid raid) => "Вернуться к гермоконсоли и закрыть герму";
    public override bool IsComplete(Raid raid) => raid.Phase == RaidPhase.Extracted;
}

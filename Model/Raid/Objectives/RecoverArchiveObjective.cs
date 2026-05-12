namespace EFP.Model.Raid.Objectives;

public sealed class RecoverArchiveObjective : RaidObjective
{
    public override RaidPhase Phase => RaidPhase.ReachObjective;
    public override string Describe(Raid raid) => "Пройти к архиву и забрать журналы";
    public override bool IsComplete(Raid raid) => raid.ObjectiveRecovered;
}

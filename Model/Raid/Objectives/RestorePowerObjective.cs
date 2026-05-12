namespace EFP.Model.Raid.Objectives;

public sealed class RestorePowerObjective : RaidObjective
{
    public override RaidPhase Phase => RaidPhase.RestorePower;
    public override string Describe(Raid raid) => "Открыть сервисную дверь и поднять рубильник";
    public override bool IsComplete(Raid raid) => raid.PowerRestored;
}

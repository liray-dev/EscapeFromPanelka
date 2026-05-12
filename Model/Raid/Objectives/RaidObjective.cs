namespace EFP.Model.Raid.Objectives;

public abstract class RaidObjective
{
    public abstract string Describe(Raid raid);
    public abstract bool IsComplete(Raid raid);
    public abstract RaidPhase Phase { get; }
}

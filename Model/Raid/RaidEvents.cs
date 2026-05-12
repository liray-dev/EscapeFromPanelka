using System.Numerics;
using EFP.Model.Raid.Props;

namespace EFP.Model.Raid;

public sealed record PlayerHealthChanged(float Current, float Max);

public sealed record MedkitsChanged(int Count);

public sealed record PhaseChanged(RaidPhase Phase);

public sealed record LootCollected(LootPickup Pickup);

public sealed record HostileDied(Hostile Hostile);

public sealed record AttackPerformed(AttackResult Attack, int HitsLanded, Vector3 Origin, Vector3 Direction, float Range);

public sealed record ContextHintChanged(string Hint);

public sealed record RaidEnded(RaidPhase FinalPhase, int CargoCount, int CargoValue);

public sealed record ContainerSearchStarted(Container Container);

public sealed record ContainerSearchProgress(Container Container, float Normalized);

public sealed record ContainerSearchCancelled(Container Container);

public sealed record ContainerOpened(Container Container);

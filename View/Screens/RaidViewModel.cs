using EFP.Model.Raid;
using EFP.Model.Raid.Props;
using RaidModel = EFP.Model.Raid.Raid;

namespace EFP.View.Screens;

public sealed class RaidViewModel : IDisposable
{
    private readonly RaidModel _raid;

    public RaidViewModel(RaidModel raid)
    {
        _raid = raid;
        PlayerMaxHealth = raid.PlayerMaxHealth;
        PlayerHealth = raid.PlayerHealth;
        MedkitCount = raid.MedkitCount;
        Phase = raid.Phase;
        ContextHint = raid.ContextHint;

        raid.PlayerHealthChanged += OnPlayerHealthChanged;
        raid.MedkitsChanged += OnMedkitsChanged;
        raid.PhaseChanged += OnPhaseChanged;
        raid.LootCollected += OnLootCollected;
        raid.HostileDied += OnHostileDied;
        raid.AttackPerformed += OnAttackPerformed;
        raid.ContextHintChanged += OnContextHintChanged;
        raid.RaidEnded += OnRaidEnded;
        raid.ContainerSearchStarted += OnContainerSearchStarted;
        raid.ContainerSearchProgress += OnContainerSearchProgress;
        raid.ContainerSearchCancelled += OnContainerSearchCancelled;
        raid.ContainerOpened += OnContainerOpened;
    }

    public float PlayerHealth { get; private set; }
    public float PlayerMaxHealth { get; private set; }
    public int MedkitCount { get; private set; }
    public RaidPhase Phase { get; private set; }
    public string ContextHint { get; private set; } = string.Empty;
    public int LootCollectedCount { get; private set; }
    public int HostileKillCount { get; private set; }
    public AttackResult? LastAttack { get; private set; }
    public int LastAttackHits { get; private set; }
    public bool RaidFinished { get; private set; }
    public int FinalCargoCount { get; private set; }
    public int FinalCargoValue { get; private set; }
    public Container? ActiveSearchContainer { get; private set; }
    public float SearchProgress { get; private set; }

    public void Dispose()
    {
        _raid.PlayerHealthChanged -= OnPlayerHealthChanged;
        _raid.MedkitsChanged -= OnMedkitsChanged;
        _raid.PhaseChanged -= OnPhaseChanged;
        _raid.LootCollected -= OnLootCollected;
        _raid.HostileDied -= OnHostileDied;
        _raid.AttackPerformed -= OnAttackPerformed;
        _raid.ContextHintChanged -= OnContextHintChanged;
        _raid.RaidEnded -= OnRaidEnded;
        _raid.ContainerSearchStarted -= OnContainerSearchStarted;
        _raid.ContainerSearchProgress -= OnContainerSearchProgress;
        _raid.ContainerSearchCancelled -= OnContainerSearchCancelled;
        _raid.ContainerOpened -= OnContainerOpened;
    }

    private void OnPlayerHealthChanged(object? sender, PlayerHealthChanged e)
    {
        PlayerHealth = e.Current;
        PlayerMaxHealth = e.Max;
    }

    private void OnMedkitsChanged(object? sender, MedkitsChanged e) => MedkitCount = e.Count;

    private void OnPhaseChanged(object? sender, PhaseChanged e) => Phase = e.Phase;

    private void OnLootCollected(object? sender, LootCollected e) => LootCollectedCount++;

    private void OnHostileDied(object? sender, HostileDied e) => HostileKillCount++;

    private void OnAttackPerformed(object? sender, AttackPerformed e)
    {
        LastAttack = e.Attack;
        LastAttackHits = e.HitsLanded;
    }

    private void OnContextHintChanged(object? sender, ContextHintChanged e) => ContextHint = e.Hint;

    private void OnRaidEnded(object? sender, RaidEnded e)
    {
        RaidFinished = true;
        FinalCargoCount = e.CargoCount;
        FinalCargoValue = e.CargoValue;
    }

    private void OnContainerSearchStarted(object? sender, ContainerSearchStarted e)
    {
        ActiveSearchContainer = e.Container;
        SearchProgress = 0f;
    }

    private void OnContainerSearchProgress(object? sender, ContainerSearchProgress e)
    {
        ActiveSearchContainer = e.Container;
        SearchProgress = e.Normalized;
    }

    private void OnContainerSearchCancelled(object? sender, ContainerSearchCancelled e)
    {
        ActiveSearchContainer = null;
        SearchProgress = 0f;
    }

    private void OnContainerOpened(object? sender, ContainerOpened e)
    {
        ActiveSearchContainer = null;
        SearchProgress = 0f;
    }
}

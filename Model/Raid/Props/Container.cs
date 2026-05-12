using System.Numerics;

namespace EFP.Model.Raid.Props;

public sealed class Container : Prop
{
    public Container(LootPickup loot, float searchDuration)
        : base($"container_{loot.Id}", loot.Label, loot.Position)
    {
        Loot = loot;
        SearchDuration = MathF.Max(0.1f, searchDuration);
    }

    public LootPickup Loot { get; }
    public ContainerState State { get; private set; } = ContainerState.Sealed;
    public float SearchDuration { get; }
    public float SearchProgress { get; private set; }
    public float NormalizedProgress => SearchDuration <= 0.0001f ? 1f : Math.Clamp(SearchProgress / SearchDuration, 0f, 1f);

    public bool StartSearch()
    {
        if (State != ContainerState.Sealed) return false;
        State = ContainerState.Searching;
        SearchProgress = 0f;
        return true;
    }

    public void CancelSearch()
    {
        if (State != ContainerState.Searching) return;
        State = ContainerState.Sealed;
        SearchProgress = 0f;
    }

    public bool Advance(float deltaTime)
    {
        if (State != ContainerState.Searching) return false;
        SearchProgress += deltaTime;
        if (SearchProgress < SearchDuration) return false;
        SearchProgress = SearchDuration;
        State = ContainerState.Open;
        return true;
    }

    public bool ConfirmLooted()
    {
        if (State != ContainerState.Open) return false;
        Loot.Collect();
        State = ContainerState.Looted;
        return true;
    }

    public override string GetInteractionPrompt()
    {
        return State switch
        {
            ContainerState.Sealed => $"E — обыскать {Loot.Label.ToLowerInvariant()}",
            ContainerState.Searching => "Обыск… не отходи",
            ContainerState.Open => $"E — забрать {Loot.Label.ToLowerInvariant()}",
            _ => string.Empty
        };
    }
}

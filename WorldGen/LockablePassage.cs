using EFP.World;

namespace EFP.WorldGen;

public sealed class LockablePassage
{
    public LockablePassage(string id, string unlockId, string label, WorldRenderable renderable, DoorState initialState,
        bool activateOnCriticalPhase = false)
    {
        Id = id;
        UnlockId = unlockId;
        Label = label;
        Renderable = renderable;
        State = initialState;
        ActivateOnCriticalPhase = activateOnCriticalPhase;
    }

    public string Id { get; }
    public string UnlockId { get; }
    public string Label { get; }
    public WorldRenderable Renderable { get; }
    public DoorState State { get; private set; }
    public bool ActivateOnCriticalPhase { get; }
    public bool Visible => State != DoorState.Open;
    public bool BlocksPassage => State is DoorState.Closed or DoorState.Locked or DoorState.Jammed;
    public bool CanInteract => State == DoorState.Closed;

    public void Unlock()
    {
        if (State == DoorState.Locked) State = DoorState.Closed;
    }

    public void Open()
    {
        if (State == DoorState.Closed) State = DoorState.Open;
    }

    public void Jam()
    {
        State = DoorState.Jammed;
    }
}
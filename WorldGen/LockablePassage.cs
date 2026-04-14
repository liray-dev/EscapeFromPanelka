using EFP.World;

namespace EFP.WorldGen;

public sealed class LockablePassage
{
    public LockablePassage(string id, string unlockId, string label, WorldRenderable renderable)
    {
        Id = id;
        UnlockId = unlockId;
        Label = label;
        Renderable = renderable;
    }

    public string Id { get; }
    public string UnlockId { get; }
    public string Label { get; }
    public WorldRenderable Renderable { get; }
}

using EFP.World;

namespace EFP.WorldGen;

public sealed class PropInstance(string propId, string sourceModuleNodeId, WorldRenderable renderable)
{
    public string PropId { get; } = propId;
    public string SourceModuleNodeId { get; } = sourceModuleNodeId;
    public WorldRenderable Renderable { get; } = renderable;
}
using EFP.World;

namespace EFP.WorldGen;

public sealed class PropInstance
{
    public PropInstance(string propId, string sourceModuleNodeId, WorldRenderable renderable)
    {
        PropId = propId;
        SourceModuleNodeId = sourceModuleNodeId;
        Renderable = renderable;
    }

    public string PropId { get; }
    public string SourceModuleNodeId { get; }
    public WorldRenderable Renderable { get; }
}
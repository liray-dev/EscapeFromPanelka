namespace EFP.Model.WorldGen;

public sealed class BlueprintStep
{
    public string NodeId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string? ParentNodeId { get; set; }
    public string ParentSocketId { get; set; } = string.Empty;
    public string ChildSocketId { get; set; } = string.Empty;
    public bool MainRoute { get; set; } = true;
}
namespace EFP.WorldGen;

public sealed class ConnectionSocketDefinition
{
    public string Id { get; set; } = string.Empty;
    public ConnectionDirection Direction { get; set; }
    public string Kind { get; set; } = "doorway";
    public float Offset { get; set; }
    public float OpeningWidth { get; set; } = 2f;
}
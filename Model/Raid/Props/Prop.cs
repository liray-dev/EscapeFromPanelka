using System.Numerics;

namespace EFP.Model.Raid.Props;

public abstract class Prop
{
    protected Prop(string id, string label, Vector3 position)
    {
        Id = id;
        Label = label;
        Position = position;
    }

    public string Id { get; }
    public string Label { get; }
    public Vector3 Position { get; }

    public virtual bool IsWithinRange(Vector3 actorPosition, float interactRadius)
    {
        var dx = actorPosition.X - Position.X;
        var dz = actorPosition.Z - Position.Z;
        return dx * dx + dz * dz <= interactRadius * interactRadius;
    }

    public abstract string GetInteractionPrompt();
}

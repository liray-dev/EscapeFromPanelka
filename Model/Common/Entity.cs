using System.Numerics;

namespace EFP.Model.Common;

public abstract class Entity
{
    protected Entity(string id, string label, Vector3 position)
    {
        Id = id;
        Label = label;
        Transform = new Transform { Position = position };
    }

    public string Id { get; }
    public string Label { get; }
    public Transform Transform { get; }
    public bool IsActive { get; protected set; } = true;

    public virtual void OnHit(float damage, Vector3 fromPosition)
    {
    }
}

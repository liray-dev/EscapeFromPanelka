using System.Numerics;
using EFP.Entities;

namespace EFP.World;

public sealed class WorldRenderable(WorldPrimitiveType primitiveType, Transform transform, Vector4 tint)
{
    public WorldPrimitiveType PrimitiveType { get; } = primitiveType;
    public Transform Transform { get; } = transform;
    public Vector4 Tint { get; } = tint;
}
using System.Numerics;
using EFP.Model.Common;

namespace EFP.Model.Raid;

public sealed class WorldRenderable(
    WorldPrimitiveType primitiveType,
    Transform transform,
    Vector4 tint,
    string? modelId = null,
    string? ownerModuleId = null)
{
    public WorldPrimitiveType PrimitiveType { get; } = primitiveType;
    public Transform Transform { get; } = transform;
    public Vector4 Tint { get; } = tint;
    public string? ModelId { get; } = modelId;
    public string? OwnerModuleId { get; } = ownerModuleId;
}
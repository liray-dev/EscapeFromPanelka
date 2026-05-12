using System.Numerics;

namespace EFP.Model.WorldGen;

public readonly record struct Placement(ModuleDefinition Definition, Vector3 Position, float RotationDegrees);

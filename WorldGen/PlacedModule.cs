using System.Numerics;

namespace EFP.WorldGen;

public sealed class PlacedModule
{
    public PlacedModule(string nodeId, ModuleDefinition definition, Vector3 position, float rotationDegrees, bool mainRoute)
    {
        NodeId = nodeId;
        Definition = definition;
        Position = position;
        RotationDegrees = rotationDegrees;
        MainRoute = mainRoute;
    }

    public string NodeId { get; }
    public ModuleDefinition Definition { get; }
    public Vector3 Position { get; }
    public float RotationDegrees { get; }
    public bool MainRoute { get; }

    public ConnectionSocketDefinition GetSocket(string socketId)
    {
        return Definition.Connections.FirstOrDefault(x => x.Id.Equals(socketId, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"Module {Definition.Id} does not contain socket {socketId}");
    }

    public Vector3 GetWorldSocketPosition(string socketId)
    {
        var socket = GetSocket(socketId);
        return Position + RotateLocal(WorldGenMath.GetSocketLocalPosition(Definition, socket), RotationDegrees);
    }

    public ConnectionDirection GetWorldSocketDirection(string socketId)
    {
        var socket = GetSocket(socketId);
        return WorldGenMath.RotateDirection(socket.Direction, RotationDegrees);
    }

    public Vector3 ToWorldPosition(Vector3 localPosition)
    {
        return Position + RotateLocal(localPosition, RotationDegrees);
    }

    public float ToWorldRotation(float localRotationDegrees)
    {
        return RotationDegrees + localRotationDegrees;
    }

    private static Vector3 RotateLocal(Vector3 value, float rotationDegrees)
    {
        var radians = MathF.PI / 180f * rotationDegrees;
        return Vector3.Transform(value, Quaternion.CreateFromAxisAngle(Vector3.UnitY, radians));
    }
}

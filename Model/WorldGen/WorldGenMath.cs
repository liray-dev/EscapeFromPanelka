using System.Numerics;

namespace EFP.Model.WorldGen;

public static class WorldGenMath
{
    public static Vector3 GetSocketLocalPosition(ModuleDefinition module, ConnectionSocketDefinition socket)
    {
        return socket.Direction switch
        {
            ConnectionDirection.North => new Vector3(socket.Offset, 0f, -module.Length * 0.5f),
            ConnectionDirection.South => new Vector3(socket.Offset, 0f, module.Length * 0.5f),
            ConnectionDirection.East => new Vector3(module.Width * 0.5f, 0f, socket.Offset),
            ConnectionDirection.West => new Vector3(-module.Width * 0.5f, 0f, socket.Offset),
            _ => Vector3.Zero
        };
    }

    public static ConnectionDirection Opposite(ConnectionDirection direction)
    {
        return direction switch
        {
            ConnectionDirection.North => ConnectionDirection.South,
            ConnectionDirection.South => ConnectionDirection.North,
            ConnectionDirection.East => ConnectionDirection.West,
            ConnectionDirection.West => ConnectionDirection.East,
            _ => direction
        };
    }

    public static ConnectionDirection RotateDirection(ConnectionDirection direction, float rotationDegrees)
    {
        var quarterTurns = NormalizeQuarterTurns((int)MathF.Round(rotationDegrees / 90f));
        var value = (int)direction;
        return (ConnectionDirection)((value + quarterTurns) % 4);
    }

    public static float SolveRotationDegrees(ConnectionDirection currentDirection, ConnectionDirection desiredDirection)
    {
        for (var i = 0; i < 4; i++)
        {
            var rotation = i * 90f;
            if (RotateDirection(currentDirection, rotation) == desiredDirection) return rotation;
        }

        return 0f;
    }

    private static int NormalizeQuarterTurns(int quarterTurns)
    {
        quarterTurns %= 4;
        if (quarterTurns < 0) quarterTurns += 4;

        return quarterTurns;
    }
}
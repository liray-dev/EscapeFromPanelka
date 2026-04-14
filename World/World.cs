using System.Numerics;
using EFP.App;
using EFP.Entities;

namespace EFP.World;

public sealed class World(GameplayConfig config)
{
    public PlayerCube Player { get; } = new(config.PlayerMoveSpeed);

    public Transform FloorTransform { get; } = new()
    {
        Position = Vector3.Zero,
        Scale = new Vector3(config.FloorSize, 1f, config.FloorSize)
    };

    public IReadOnlyList<Transform> DebugBlocks { get; } =
    [
        new() { Position = new Vector3(4f, 1f, -2f), Scale = new Vector3(2f, 2f, 2f) },
        new() { Position = new Vector3(-5f, 1.5f, 3f), Scale = new Vector3(2f, 3f, 1.5f) },
        new() { Position = new Vector3(0f, 1f, 6f), Scale = new Vector3(6f, 2f, 0.75f) },
        new() { Position = new Vector3(-7f, 2f, -6f), Scale = new Vector3(1.5f, 4f, 1.5f) }
    ];

    public void Tick(float deltaTime, Vector2 movementInput)
    {
        Player.Move(movementInput, deltaTime);
    }
}
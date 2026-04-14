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

    public void Tick(float deltaTime, Vector2 movementInput)
    {
        Player.Move(movementInput, deltaTime);
    }
}
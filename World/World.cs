using System.Numerics;
using EFP.App;
using EFP.Entities;
using EFP.WorldGen;

namespace EFP.World;

public sealed class World
{
    public World(GameplayConfig config, ProceduralSector sector)
    {
        Sector = sector;
        Player = new PlayerCube(config.PlayerMoveSpeed, sector.PlayerSpawn);

        var halfExtents = Vector3.Max(Vector3.Abs(sector.BoundsMin), Vector3.Abs(sector.BoundsMax));
        var foundationSize = MathF.Max(halfExtents.X, halfExtents.Z) * 2f + 6f;
        Foundation = new WorldRenderable(
            WorldPrimitiveType.Cube,
            new Transform
            {
                Position = new Vector3(0f, -0.08f, 0f),
                Scale = new Vector3(foundationSize, 0.16f, foundationSize)
            },
            new Vector4(0.11f, 0.12f, 0.13f, 1f));
    }

    public ProceduralSector Sector { get; }
    public PlayerCube Player { get; }
    public WorldRenderable Foundation { get; }
    public IReadOnlyList<WorldRenderable> StaticGeometry => Sector.StaticGeometry;
    public IReadOnlyList<PropInstance> Props => Sector.Props;
    public int Seed => Sector.Seed;
    public int ModuleCount => Sector.Modules.Count;
    public int PropCount => Sector.Props.Count;

    public void Tick(float deltaTime, Vector2 movementInput)
    {
        Player.Move(movementInput, deltaTime);
    }
}
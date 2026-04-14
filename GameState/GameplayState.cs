using System.Numerics;
using EFP.App;
using EFP.Scene;
using EFP.UI.Screens;
using EFP.WorldGen;
using Silk.NET.Input;

namespace EFP.GameState;

public sealed class GameplayState : IGameState
{
    private const string ModuleLibraryPath = "config/worldgen/module_library.json";

    private readonly IGameStateContext _context;
    private readonly GameplayConfig _gameplayConfig;
    private readonly HudScreen _hudScreen;
    private readonly StructureBlueprintGenerator _blueprintGenerator = new();
    private readonly StructureAssembler _structureAssembler = new();

    private World.World? _world;
    private TopDownCamera? _camera;
    private ModuleLibrary? _moduleLibrary;
    private Vector2 _movementInput;
    private int _seed;

    public GameplayState(IGameStateContext context)
    {
        _context = context;
        _gameplayConfig = context.Config.Gameplay;
        _hudScreen = new HudScreen(context);
    }

    public string Name => "GameplayState";

    public void Enter()
    {
        _moduleLibrary = new ModuleLibrary(_context.Resources.LoadConfig<ModuleLibraryConfig>(ModuleLibraryPath));
        _camera = new TopDownCamera(_context.Config.Camera);

        _seed = Environment.TickCount & int.MaxValue;
        RebuildWorld(_seed);
    }

    public void Exit()
    {
    }

    public void HandleInput()
    {
    }

    public void Update(double deltaTime)
    {
        if (_world is null || _camera is null)
        {
            return;
        }

        var input = _context.Input;

        if (input.IsKeyPressed(Key.Escape))
        {
            _context.RequestStateChange(AppStateId.MainMenu);
            return;
        }

        if (input.IsKeyPressed(Key.R))
        {
            RebuildWorld(unchecked(_seed + 1));
            return;
        }

        _movementInput = ReadMovementInput(input);

        if (input.IsMouseDown(MouseButton.Right))
        {
            _world.Player.RotateYaw(input.MouseDelta.X * _gameplayConfig.PlayerRotationSensitivity);
        }

        _camera.Follow(_world.Player.Transform.Position);
    }

    public void FixedUpdate(double fixedDeltaTime)
    {
        _world?.Tick((float)fixedDeltaTime, _movementInput, _context.Input.IsKeyPressed(Key.E));
    }

    public void Render(float alpha)
    {
        if (_world is null || _camera is null)
        {
            return;
        }

        _context.SceneRenderer.RenderWorld(_world, _camera);
    }

    public void RenderUi()
    {
        if (_world is null)
        {
            return;
        }

        _hudScreen.Draw(Name, _world, _context.FrameStats);
    }

    public void Resize(int width, int height)
    {
        _camera?.Resize(width, height);
    }

    public void Dispose()
    {
    }

    private void RebuildWorld(int seed)
    {
        if (_moduleLibrary is null || _camera is null)
        {
            return;
        }

        _seed = seed;
        var blueprint = _blueprintGenerator.Generate(seed);
        var sector = _structureAssembler.Assemble(blueprint, _moduleLibrary);
        _world = new World.World(_gameplayConfig, sector);
        _camera.Follow(_world.Player.Transform.Position);
    }

    private static Vector2 ReadMovementInput(Input.InputService input)
    {
        var movement = Vector2.Zero;

        if (input.IsKeyDown(Key.W)) movement.Y -= 1f;
        if (input.IsKeyDown(Key.S)) movement.Y += 1f;
        if (input.IsKeyDown(Key.A)) movement.X -= 1f;
        if (input.IsKeyDown(Key.D)) movement.X += 1f;

        return movement;
    }
}
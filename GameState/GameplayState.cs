using System.Numerics;
using EFP.App;
using EFP.Input;
using EFP.Scene;
using EFP.UI.Screens;
using EFP.WorldGen;
using Silk.NET.Input;

namespace EFP.GameState;

public sealed class GameplayState(IGameStateContext context) : IGameState
{
    private const string ModuleLibraryPath = "config/worldgen/module_library.json";
    private readonly StructureBlueprintGenerator _blueprintGenerator = new();
    private readonly GameplayConfig _gameplayConfig = context.Config.Gameplay;
    private readonly HudScreen _hudScreen = new(context);
    private readonly StructureAssembler _structureAssembler = new();
    private TopDownCamera? _camera;
    private bool _lastQuietMovement;
    private float _lastRoomSizeMultiplier = 1f;
    private ModuleLibraryConfig? _moduleConfig;
    private Vector2 _movementInput;
    private bool _queuedInteract;
    private bool _queuedUseMedkit;
    private int _seed;
    private World.World? _world;

    public string Name => "GameplayState";

    public void Enter()
    {
        _moduleConfig = context.Resources.LoadConfig<ModuleLibraryConfig>(ModuleLibraryPath);
        _camera = new TopDownCamera(context.Config.Camera);
        _camera.ApplyDebug(context.DebugSettings);

        _seed = Environment.TickCount & int.MaxValue;
        _lastRoomSizeMultiplier = context.DebugSettings.RoomSizeMultiplier;
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
        if (_world is null || _camera is null) return;

        var input = context.Input;
        var captureKeyboard = context.DebugSettings.CaptureKeyboard;
        var captureMouse = context.DebugSettings.CaptureMouse;

        _world.IgnoreCollision = context.DebugSettings.IgnoreCollisions;

        if (!captureKeyboard && input.IsKeyPressed(Key.Escape))
        {
            context.RequestStateChange(AppStateId.MainMenu);
            return;
        }

        if (!captureKeyboard && input.IsKeyPressed(Key.R))
        {
            RebuildWorld(unchecked(_seed + 1));
            return;
        }

        if (MathF.Abs(context.DebugSettings.RoomSizeMultiplier - _lastRoomSizeMultiplier) > 0.001f)
        {
            _lastRoomSizeMultiplier = context.DebugSettings.RoomSizeMultiplier;
            RebuildWorld(_seed);
            return;
        }

        _movementInput = captureKeyboard ? Vector2.Zero : ReadMovementInput(input);
        _lastQuietMovement = !captureKeyboard && (input.IsKeyDown(Key.ShiftLeft) || input.IsKeyDown(Key.ShiftRight));
        if (!captureKeyboard && input.IsKeyPressed(Key.E)) _queuedInteract = true;
        if (!captureKeyboard && input.IsKeyPressed(Key.Q)) _queuedUseMedkit = true;

        if (!captureMouse && input.IsMouseDown(MouseButton.Right))
        {
            var rotationSpeed = _gameplayConfig.PlayerRotationSensitivity *
                                context.DebugSettings.RotationSpeedMultiplier;
            _world.Player.RotateYaw(input.MouseDelta.X * rotationSpeed);
        }

        _camera.ApplyDebug(context.DebugSettings);
        _camera.Follow(_world.Player.Transform.Position, context.DebugSettings.CameraFollowSmoothing, (float)deltaTime);
    }

    public void FixedUpdate(double fixedDeltaTime)
    {
        if (_world is null) return;

        var interact = _queuedInteract;
        var useMedkit = _queuedUseMedkit;
        _queuedInteract = false;
        _queuedUseMedkit = false;

        _world.Tick(
            (float)fixedDeltaTime,
            _movementInput,
            interact,
            context.DebugSettings.AllowCriticalMutation,
            _lastQuietMovement,
            useMedkit);
    }

    public void Render(float alpha)
    {
        if (_world is null || _camera is null) return;

        context.SceneRenderer.RenderWorld(_world, _camera, context.DebugSettings);
    }

    public void RenderUi()
    {
        if (_world is null) return;

        _hudScreen.Draw(Name, _world, context.FrameStats);
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
        if (_moduleConfig is null || _camera is null) return;

        _seed = seed;
        var roomSizeMultiplier = Math.Clamp(context.DebugSettings.RoomSizeMultiplier, 0.75f, 1.80f);
        var library = new ModuleLibrary(_moduleConfig, roomSizeMultiplier);
        var blueprint = StructureBlueprintGenerator.Generate(seed);
        var sector = StructureAssembler.Assemble(blueprint, library);
        _world = new World.World(_gameplayConfig, sector)
        {
            IgnoreCollision = context.DebugSettings.IgnoreCollisions
        };
        _queuedInteract = false;
        _queuedUseMedkit = false;
        _camera.SnapTo(_world.Player.Transform.Position);
    }

    private static Vector2 ReadMovementInput(InputService input)
    {
        var movement = Vector2.Zero;

        if (input.IsKeyDown(Key.W)) movement.Y -= 1f;
        if (input.IsKeyDown(Key.S)) movement.Y += 1f;
        if (input.IsKeyDown(Key.A)) movement.X -= 1f;
        if (input.IsKeyDown(Key.D)) movement.X += 1f;

        return movement;
    }
}
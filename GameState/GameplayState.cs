using System.Numerics;
using EFP.App;
using EFP.Input;
using EFP.Scene;
using EFP.UI.Screens;
using Silk.NET.Input;

namespace EFP.GameState;

public sealed class GameplayState(IGameStateContext context) : IGameState
{
    private readonly GameplayConfig _gameplayConfig = context.Config.Gameplay;
    private readonly HudScreen _hudScreen = new(context);
    private TopDownCamera? _camera;
    private Vector2 _movementInput;

    private World.World? _world;

    public string Name => "GameplayState";

    public void Enter()
    {
        _world = new World.World(_gameplayConfig);
        _camera = new TopDownCamera(context.Config.Camera);
        _camera.Resize(context.Window.Size.X, context.Window.Size.Y);
        _camera.Follow(_world.Player.Transform.Position);
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

        if (input.IsKeyPressed(Key.Escape))
        {
            context.RequestStateChange(AppStateId.MainMenu);
            return;
        }

        _movementInput = ReadMovementInput(input);

        if (input.IsMouseDown(MouseButton.Right))
            _world.Player.RotateYaw(input.MouseDelta.X * _gameplayConfig.PlayerRotationSensitivity);

        _camera.Follow(_world.Player.Transform.Position);
    }

    public void FixedUpdate(double fixedDeltaTime)
    {
        _world?.Tick((float)fixedDeltaTime, _movementInput);
    }

    public void Render(float alpha)
    {
        if (_world is null || _camera is null) return;

        context.SceneRenderer.RenderWorld(_world, _camera);
    }

    public void RenderUi()
    {
        if (_world is null) return;

        _hudScreen.Draw(Name, _world.Player, context.FrameStats);
    }

    public void Resize(int width, int height)
    {
        _camera?.Resize(width, height);
    }

    public void Dispose()
    {
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
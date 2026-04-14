using System.Numerics;
using EFP.App;
using EFP.Input;
using EFP.Scene;
using EFP.UI;
using Silk.NET.Input;

namespace EFP.GameState;

public sealed class GameplayState(IGameStateContext context) : IGameState
{
    private readonly GameplayConfig _gameplayConfig = context.Config.Gameplay;
    private readonly HudRenderer _hudRenderer = new();

    private World.World _world = null!;
    private TopDownCamera _camera = null!;
    private Vector2 _movementInput;

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
        var input = context.Input;

        if (input.IsKeyPressed(Key.Escape))
        {
            context.RequestStateChange(AppStateId.MainMenu);
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
        _world.Tick((float)fixedDeltaTime, _movementInput);
        _camera.Follow(_world.Player.Transform.Position);
    }

    public void Render(double alpha)
    {
        context.SceneRenderer.RenderWorld(_world, _camera);
    }

    public void RenderUi()
    {
        HudRenderer.Draw(Name, _world.Player, context.FrameStats);
    }

    public void Resize(int width, int height)
    {
        _camera.Resize(width, height);
    }

    public void Dispose()
    {
    }

    private static Vector2 ReadMovementInput(InputService input)
    {
        var horizontal = 0f;
        var vertical = 0f;

        if (input.IsKeyDown(Key.A))
        {
            horizontal -= 1f;
        }

        if (input.IsKeyDown(Key.D))
        {
            horizontal += 1f;
        }

        if (input.IsKeyDown(Key.W))
        {
            vertical -= 1f;
        }

        if (input.IsKeyDown(Key.S))
        {
            vertical += 1f;
        }

        var vector = new Vector2(horizontal, vertical);
        if (vector.LengthSquared() > 1f)
        {
            vector = Vector2.Normalize(vector);
        }

        return vector;
    }
}
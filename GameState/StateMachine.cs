namespace EFP.GameState;

public sealed class StateMachine : IDisposable
{
    private IGameState? _currentState;

    public string CurrentStateName => _currentState?.Name ?? "None";

    public void Dispose()
    {
        _currentState?.Exit();
        _currentState?.Dispose();
        _currentState = null;
    }

    public void ChangeState(IGameState nextState)
    {
        _currentState?.Exit();
        _currentState?.Dispose();
        _currentState = nextState;
        _currentState.Enter();
    }

    public void HandleInput()
    {
        _currentState?.HandleInput();
    }

    public void Update(double deltaTime)
    {
        _currentState?.Update(deltaTime);
    }

    public void FixedUpdate(double fixedDeltaTime)
    {
        _currentState?.FixedUpdate(fixedDeltaTime);
    }

    public void Render(float alpha)
    {
        _currentState?.Render(alpha);
    }

    public void RenderUi()
    {
        _currentState?.RenderUi();
    }

    public void Resize(int width, int height)
    {
        _currentState?.Resize(width, height);
    }
}
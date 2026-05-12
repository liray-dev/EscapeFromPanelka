using EFP.App;
using EFP.View.Screens;

namespace EFP.GameState;

public sealed class MainMenuState(IGameStateContext context) : IGameState
{
    private readonly MainMenuScreen _screen = new(context,
        () => context.RequestStateChange(AppStateId.Lobby),
        context.RequestExit);

    public string Name => "MainMenuState";

    public void Enter()
    {
        _screen.OnOpen();
    }

    public void Exit()
    {
        _screen.OnClose();
    }

    public void HandleInput()
    {
    }

    public void Update(double deltaTime)
    {
        _screen.Update(deltaTime);
    }

    public void FixedUpdate(double fixedDeltaTime)
    {
        _screen.FixedUpdate(fixedDeltaTime);
    }

    public void Render(float alpha)
    {
    }

    public void RenderUi()
    {
        _screen.Draw();
    }

    public void Resize(int width, int height)
    {
    }

    public void Dispose()
    {
    }
}
using EFP.App;
using EFP.View.Screens;

namespace EFP.GameState;

public sealed class LobbyState(IGameStateContext context) : IGameState
{
    private readonly LobbyScreen _screen = new(context,
        () => context.RequestStateChange(AppStateId.Gameplay),
        () => context.RequestStateChange(AppStateId.MainMenu));

    public string Name => "LobbyState";

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

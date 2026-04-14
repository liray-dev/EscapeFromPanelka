using EFP.App;
using EFP.UI;

namespace EFP.GameState;

public sealed class MainMenuState(IGameStateContext context) : IGameState
{
    private readonly MainMenuView _mainMenuView = new();

    public string Name => "MainMenuState";

    public void Enter()
    {
    }

    public void Exit()
    {
    }

    public void HandleInput()
    {
    }

    public void Update(double deltaTime)
    {
    }

    public void FixedUpdate(double fixedDeltaTime)
    {
    }

    public void Render(double alpha)
    {
    }

    public void RenderUi()
    {
        MainMenuView.Draw(
            () => context.RequestStateChange(AppStateId.Gameplay),
            context.RequestExit);
    }

    public void Resize(int width, int height)
    {
    }

    public void Dispose()
    {
    }
}

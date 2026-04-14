namespace EFP.GameState;

public interface IGameState : IDisposable
{
    string Name { get; }
    void Enter();
    void Exit();
    void HandleInput();
    void Update(double deltaTime);
    void FixedUpdate(double fixedDeltaTime);
    void Render(double alpha);
    void RenderUi();
    void Resize(int width, int height);
}
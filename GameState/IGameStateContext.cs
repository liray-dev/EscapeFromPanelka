using EFP.App;
using EFP.Input;
using EFP.Rendering;
using EFP.Utilities;
using Silk.NET.Windowing;

namespace EFP.GameState;

public interface IGameStateContext
{
    GameConfig Config { get; }
    InputService Input { get; }
    SceneRenderer SceneRenderer { get; }
    FrameStats FrameStats { get; }
    IWindow Window { get; }
    void RequestStateChange(AppStateId nextState);
    void RequestExit();
}
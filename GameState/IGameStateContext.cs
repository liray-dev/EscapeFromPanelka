using EFP.App;
using EFP.Input;
using EFP.Rendering;
using EFP.Resources;
using EFP.Utilities;
using Silk.NET.Windowing;

namespace EFP.GameState;

public interface IGameStateContext
{
    GameConfig Config { get; }
    DebugSettings DebugSettings { get; }
    InputService Input { get; }
    IWindow Window { get; }
    SceneRenderer SceneRenderer { get; }
    UiRenderer UiRenderer { get; }
    GameResources Resources { get; }
    FrameStats FrameStats { get; }
    void RequestStateChange(AppStateId nextState);
    void RequestExit();
}
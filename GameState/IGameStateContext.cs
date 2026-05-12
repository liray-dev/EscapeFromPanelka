using EFP.App;
using EFP.Model.Profile;
using EFP.Platform;
using EFP.Utilities;
using EFP.View.Rendering;
using EFP.View.Rendering.Models;
using EFP.View.Resources;
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
    ModelRegistry ModelRegistry { get; }
    FrameStats FrameStats { get; }
    PlayerProfile Profile { get; }
    void RequestStateChange(AppStateId nextState);
    void RequestExit();
    void SaveProfile();
}

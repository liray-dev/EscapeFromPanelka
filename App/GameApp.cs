using System.Numerics;
using EFP.GameState;
using EFP.Input;
using EFP.Platform;
using EFP.Rendering;
using EFP.Utilities;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace EFP.App;

public sealed class GameApp : IGameStateContext, IDisposable
{
    private readonly GameHostWindow _hostWindow;
    private readonly StateMachine _stateMachine = new();
    private readonly FrameStats _frameStats = new();

    private GL? _gl;
    private IInputContext? _inputContext;
    private InputService? _input;
    private SceneRenderer? _sceneRenderer;
    private ImGuiController? _imguiController;
    private AppStateId? _pendingStateChange;
    private bool _pendingExit;
    private bool _isLoaded;
    private bool _disposed;
    private double _tickAccumulator;

    public GameApp(GameConfig config)
    {
        Config = config;
        _hostWindow = new GameHostWindow(config.Window);

        var window = _hostWindow.Window;
        window.Load += OnLoad;
        window.Render += OnRender;
        window.FramebufferResize += OnFramebufferResize;
    }

    public GameConfig Config { get; }

    public InputService Input => _input ?? throw new InvalidOperationException("Input service is not initialized.");

    public SceneRenderer SceneRenderer =>
        _sceneRenderer ?? throw new InvalidOperationException("Scene renderer is not initialized.");

    public FrameStats FrameStats => _frameStats;

    public IWindow Window => _hostWindow.Window;

    public void Run()
    {
        _hostWindow.Run();
    }

    public void RequestStateChange(AppStateId nextState)
    {
        _pendingStateChange = nextState;
    }

    public void RequestExit()
    {
        _pendingExit = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _stateMachine.Dispose();
        _imguiController?.Dispose();
        _sceneRenderer?.Dispose();
        _inputContext?.Dispose();
        _gl?.Dispose();
        _hostWindow.Dispose();
    }

    private void OnLoad()
    {
        var window = _hostWindow.Window;
        window.Center();

        _gl = window.CreateOpenGL();
        _inputContext = window.CreateInput();
        _input = new InputService(_inputContext);
        _sceneRenderer = new SceneRenderer(_gl);
        _sceneRenderer.Load();
        _imguiController = new ImGuiController(_gl, window, _inputContext);

        OnFramebufferResize(window.Size);
        _stateMachine.ChangeState(CreateState(AppStateId.MainMenu));
        _isLoaded = true;
    }

    private void OnRender(double deltaTime)
    {
        if (!_isLoaded || _input is null || _sceneRenderer is null || _imguiController is null)
        {
            return;
        }

        _input.BeginFrame();
        _frameStats.Update(deltaTime);

        _stateMachine.HandleInput();
        _stateMachine.Update(deltaTime);

        _tickAccumulator += deltaTime;
        while (_tickAccumulator >= FixedDeltaTime)
        {
            _stateMachine.FixedUpdate(FixedDeltaTime);
            _tickAccumulator -= FixedDeltaTime;
        }

        _imguiController.Update((float)Math.Max(deltaTime, 1.0 / 1000.0));
        _sceneRenderer.BeginFrame(new Vector4(0.06f, 0.07f, 0.09f, 1.0f));
        _stateMachine.Render(_tickAccumulator / FixedDeltaTime);
        _stateMachine.RenderUi();
        _imguiController.Render();

        _input.EndFrame();
        ApplyPendingActions();
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        _sceneRenderer?.Resize(size.X, size.Y);
        _stateMachine.Resize(size.X, size.Y);
    }

    private void ApplyPendingActions()
    {
        if (_pendingExit)
        {
            _pendingExit = false;
            _hostWindow.Window.Close();
            return;
        }

        if (_pendingStateChange is { } nextState)
        {
            _pendingStateChange = null;
            _stateMachine.ChangeState(CreateState(nextState));
        }
    }

    private IGameState CreateState(AppStateId stateId)
    {
        return stateId switch
        {
            AppStateId.MainMenu => new MainMenuState(this),
            AppStateId.Gameplay => new GameplayState(this),
            _ => throw new ArgumentOutOfRangeException(nameof(stateId), stateId, null)
        };
    }

    private double FixedDeltaTime => 1.0 / Math.Max(1, Config.Gameplay.FixedTickRate);
}
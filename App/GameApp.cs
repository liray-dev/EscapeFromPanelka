using EFP.GameState;
using EFP.Input;
using EFP.Platform;
using EFP.Rendering;
using EFP.Resources;
using EFP.Utilities;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace EFP.App;

public sealed class GameApp : IGameStateContext, IDisposable
{
    private readonly GameHostWindow _hostWindow;
    private readonly StateMachine _stateMachine = new();
    private DebugOverlay? _debugOverlay;
    private bool _disposed;

    private GL? _gl;
    private InputService? _input;
    private bool _isLoaded;
    private bool _pendingExit;
    private AppStateId? _pendingStateChange;
    private GameResources? _resources;
    private SceneRenderer? _sceneRenderer;
    private double _tickAccumulator;
    private UiRenderer? _uiRenderer;

    public GameApp(GameConfig config)
    {
        Config = config;
        _hostWindow = new GameHostWindow(config.Window);

        var window = _hostWindow.Window;
        window.Load += OnLoad;
        window.Render += OnRender;
        window.FramebufferResize += OnFramebufferResize;
    }

    private double FixedDeltaTime => 1.0 / Math.Max(1, Config.Gameplay.FixedTickRate);

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        _stateMachine.Dispose();
        _debugOverlay?.Dispose();
        _uiRenderer?.Dispose();
        _sceneRenderer?.Dispose();
        _resources?.Dispose();
        _input?.Dispose();
        _gl?.Dispose();
        _hostWindow.Dispose();
    }

    public GameConfig Config { get; }
    public DebugSettings DebugSettings { get; } = new();
    public InputService Input => _input ?? throw new InvalidOperationException("Input service is not initialized.");

    public SceneRenderer SceneRenderer =>
        _sceneRenderer ?? throw new InvalidOperationException("Scene renderer is not initialized.");

    public UiRenderer UiRenderer =>
        _uiRenderer ?? throw new InvalidOperationException("UI renderer is not initialized.");

    public GameResources Resources =>
        _resources ?? throw new InvalidOperationException("Resources are not initialized.");

    public FrameStats FrameStats { get; } = new();

    public IWindow Window => _hostWindow.Window;

    public void RequestStateChange(AppStateId nextState)
    {
        _pendingStateChange = nextState;
    }

    public void RequestExit()
    {
        _pendingExit = true;
    }

    public void Run()
    {
        _hostWindow.Run();
    }

    private void OnLoad()
    {
        var window = _hostWindow.Window;
        window.Center();

        _gl = window.CreateOpenGL();
        var inputContext = window.CreateInput();
        _input = new InputService(inputContext);
        _debugOverlay = new DebugOverlay(_gl, window, inputContext, DebugSettings);

        _resources = new GameResources(Path.Combine(AppContext.BaseDirectory, "assets"));
        _resources.Initialize(_gl);

        _sceneRenderer = new SceneRenderer(_gl, _resources);
        _sceneRenderer.Load();

        _uiRenderer = new UiRenderer(_gl, _resources);
        _uiRenderer.Load();

        OnFramebufferResize(window.Size);
        _stateMachine.ChangeState(CreateState(AppStateId.MainMenu));
        _isLoaded = true;
    }

    private void OnRender(double deltaTime)
    {
        if (!_isLoaded || _gl is null || _input is null || _sceneRenderer is null || _uiRenderer is null) return;

        _input.BeginFrame();

        if (_input.IsKeyPressed(Key.F1)) DebugSettings.Enabled = !DebugSettings.Enabled;

        FrameStats.Update(deltaTime);
        _debugOverlay?.Update((float)deltaTime, _stateMachine.CurrentStateName, FrameStats);

        _stateMachine.HandleInput();
        _stateMachine.Update(deltaTime);

        _tickAccumulator += deltaTime;
        while (_tickAccumulator >= FixedDeltaTime)
        {
            _stateMachine.FixedUpdate(FixedDeltaTime);
            _tickAccumulator -= FixedDeltaTime;
        }

        _sceneRenderer.BeginFrame();
        _stateMachine.Render((float)(_tickAccumulator / FixedDeltaTime));

        _uiRenderer.BeginFrame(Window.Size.X, Window.Size.Y);
        _stateMachine.RenderUi();
        _uiRenderer.EndFrame();
        _debugOverlay?.Render();

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
}
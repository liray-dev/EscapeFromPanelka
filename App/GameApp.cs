using EFP.GameState;
using EFP.Model.Profile;
using EFP.Platform;
using EFP.Utilities;
using EFP.View.Rendering;
using EFP.View.Rendering.Models;
using EFP.View.Resources;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace EFP.App;

public sealed class GameApp : IGameStateContext, IDisposable
{
    private readonly string? _configPath;
    private readonly GameHostWindow _hostWindow;
    private readonly string _profilePath;
    private readonly StateMachine _stateMachine = new();
    private DebugOverlay? _debugOverlay;
    private bool _disposed;

    private GL? _gl;
    private InputService? _input;
    private bool _isLoaded;
    private ModelRegistry? _modelRegistry;
    private bool _pendingExit;
    private AppStateId? _pendingStateChange;
    private GameResources? _resources;
    private SceneRenderer? _sceneRenderer;
    private double _tickAccumulator;
    private UiRenderer? _uiRenderer;

    public GameApp(GameConfig config, string? configPath = null)
    {
        _configPath = configPath;
        Config = config;
        _profilePath = ResolveProfilePath(configPath);
        Profile = ProfileStore.LoadOrCreate(_profilePath);
        ApplyDebugDefaults(config);
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
        _modelRegistry?.Dispose();
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

    public ModelRegistry ModelRegistry =>
        _modelRegistry ?? throw new InvalidOperationException("Model registry is not initialized.");

    public FrameStats FrameStats { get; } = new();
    public PlayerProfile Profile { get; }

    public IWindow Window => _hostWindow.Window;

    public void SaveProfile()
    {
        ProfileStore.Save(_profilePath, Profile);
    }

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
        _debugOverlay = new DebugOverlay(_gl, window, inputContext, DebugSettings, SaveDebugSettings);

        _resources = new GameResources(Path.Combine(AppContext.BaseDirectory, "assets"));
        _resources.Initialize(_gl);

        _modelRegistry = new ModelRegistry(_gl, _resources);
        _modelRegistry.RegisterDefaults();

        _sceneRenderer = new SceneRenderer(_gl, _resources, _modelRegistry);
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

        if (_input.IsKeyPressed(Key.F1))
        {
            DebugSettings.Enabled = !DebugSettings.Enabled;
            SaveDebugSettings();
        }

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
            AppStateId.Lobby => new LobbyState(this),
            AppStateId.Gameplay => new GameplayState(this),
            _ => throw new ArgumentOutOfRangeException(nameof(stateId), stateId, null)
        };
    }

    private static string ResolveProfilePath(string? configPath)
    {
        var directory = string.IsNullOrWhiteSpace(configPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EscapeFromPanelka")
            : Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory;
        return Path.Combine(directory, "profile.json");
    }

    private void ApplyDebugDefaults(GameConfig config)
    {
        config.Debug.ApplyTo(DebugSettings);
    }

    private void SaveDebugSettings()
    {
        if (string.IsNullOrWhiteSpace(_configPath)) return;

        Config.Debug.CaptureFrom(DebugSettings);
        GameConfigLoader.Save(_configPath, Config);

        var sourceConfigPath = TryFindSourceConfigPath();
        if (!string.IsNullOrWhiteSpace(sourceConfigPath)
            && !Path.GetFullPath(sourceConfigPath).Equals(Path.GetFullPath(_configPath),
                StringComparison.OrdinalIgnoreCase))
            GameConfigLoader.Save(sourceConfigPath, Config);
    }

    private static string? TryFindSourceConfigPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "EFP.csproj")))
                return Path.Combine(current.FullName, "assets", "config", "gameconfig.json");

            current = current.Parent;
        }

        return null;
    }
}

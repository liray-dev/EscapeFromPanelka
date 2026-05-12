using EFP.App;
using EFP.Controller;
using EFP.Model.Inventory;
using EFP.Model.Raid;
using EFP.Model.WorldGen;
using EFP.View.Camera;
using EFP.View.Rendering;
using EFP.View.Screens;
using RaidModel = EFP.Model.Raid.Raid;

namespace EFP.GameState;

public sealed class GameplayState(IGameStateContext context) : IGameState
{
    private readonly HudScreen _hudScreen = new(context);
    private TopDownCamera? _camera;
    private RaidController? _controller;
    private float _lastRoomSizeMultiplier = 1f;
    private RaidModel? _raid;
    private RaidSettings _settings = RaidSettings.Default;
    private int _seed;
    private MeleeTracerView? _tracerView;
    private RaidViewModel? _viewModel;

    public string Name => "GameplayState";

    public void Enter()
    {
        _settings = BuildSettings(context.Config.Gameplay);
        _camera = new TopDownCamera(context.Config.Camera);
        _camera.ApplyDebug(context.DebugSettings);

        _seed = Environment.TickCount & int.MaxValue;
        _lastRoomSizeMultiplier = context.DebugSettings.RoomSizeMultiplier;
        _controller = new RaidController(context.Input, context.DebugSettings,
            context.Config.Gameplay.PlayerRotationSensitivity);

        RebuildRaid(_seed);
    }

    public void Exit()
    {
        DepositOnExtract();
        _tracerView?.Dispose();
        _tracerView = null;
        _viewModel?.Dispose();
        _viewModel = null;
    }

    private void DepositOnExtract()
    {
        if (_raid is null || _raid.Phase != RaidPhase.Extracted) return;

        var stash = context.Profile.Stash;
        foreach (var pickup in _raid.Sector.Loot)
        {
            if (!pickup.Collected) continue;
            var item = InventoryCatalog.ResolveLoot(pickup);
            if (item is null) continue;
            stash.TryAdd(item);
        }

        if (_raid.ObjectiveRecovered)
        {
            var archive = InventoryCatalog.Resolve("archive_packet");
            if (archive is not null) stash.TryAdd(archive);
        }

        context.Profile.AddCurrency(_raid.CargoValue);
        context.SaveProfile();
    }

    public void HandleInput()
    {
    }

    public void Update(double deltaTime)
    {
        if (_raid is null || _camera is null || _controller is null) return;

        if (MathF.Abs(context.DebugSettings.RoomSizeMultiplier - _lastRoomSizeMultiplier) > 0.001f)
        {
            _lastRoomSizeMultiplier = context.DebugSettings.RoomSizeMultiplier;
            RebuildRaid(_seed);
            return;
        }

        _controller.PollInput(_raid);

        if (_controller.MenuRequested)
        {
            context.RequestStateChange(AppStateId.Lobby);
            return;
        }

        if (_controller.RebuildRequested)
        {
            RebuildRaid(unchecked(_seed + 1));
            return;
        }

        _camera.ApplyDebug(context.DebugSettings);
        _camera.Follow(_raid.Player.Transform.Position, context.DebugSettings.CameraFollowSmoothing,
            (float)deltaTime);
        _tracerView?.Update((float)deltaTime);
    }

    public void FixedUpdate(double fixedDeltaTime)
    {
        if (_raid is null || _controller is null) return;
        _controller.Tick(_raid, (float)fixedDeltaTime);
    }

    public void Render(float alpha)
    {
        if (_raid is null || _camera is null) return;
        context.SceneRenderer.RenderWorld(_raid, _camera, context.DebugSettings);
        _tracerView?.Render(context.SceneRenderer, _camera);
    }

    public void RenderUi()
    {
        if (_raid is null || _viewModel is null) return;
        _hudScreen.Draw(Name, _raid, _viewModel, context.FrameStats);
    }

    public void Resize(int width, int height)
    {
        _camera?.Resize(width, height);
    }

    public void Dispose()
    {
        _tracerView?.Dispose();
        _tracerView = null;
        _viewModel?.Dispose();
        _viewModel = null;
    }

    private void RebuildRaid(int seed)
    {
        if (_camera is null) return;

        _seed = seed;
        var roomSizeMultiplier = Math.Clamp(context.DebugSettings.RoomSizeMultiplier, 0.75f, 1.80f);
        var library = new ModuleLibrary(roomSizeMultiplier);
        var state = StructureBlueprintGenerator.GenerateWithState(seed, library,
            _settings.MinModulesPerSector, _settings.MaxModulesPerSector);
        var sector = StructureAssembler.Assemble(state.Blueprint, library);
        var grower = new SectorGrower(state, library, seed, _settings.ModuleCap);

        _tracerView?.Dispose();
        _viewModel?.Dispose();
        _raid = new RaidModel(_settings, sector)
        {
            IgnoreCollision = context.DebugSettings.IgnoreCollisions
        };
        _raid.AttachGrower(grower);
        _viewModel = new RaidViewModel(_raid);
        _tracerView = new MeleeTracerView(_raid);
        _camera.SnapTo(_raid.Player.Transform.Position);
    }

    private static RaidSettings BuildSettings(GameplayConfig config)
    {
        return new RaidSettings
        {
            PlayerMoveSpeed = config.PlayerMoveSpeed,
            PlayerCollisionRadius = config.PlayerCollisionRadius,
            PlayerMaxHealth = config.PlayerMaxHealth,
            StartingMedkits = config.StartingMedkits,
            MedkitHealAmount = config.MedkitHealAmount,
            InteractRadius = config.InteractRadius,
            RaidDurationSeconds = config.RaidDurationSeconds,
            PressureThresholdSeconds = config.PressureThresholdSeconds,
            CriticalThresholdSeconds = config.CriticalThresholdSeconds,
            QuietWalkMultiplier = config.QuietWalkMultiplier,
            NoiseDecayRate = config.NoiseDecayRate,
            HostileContactDamage = config.HostileContactDamage,
            HostileContactCooldownSeconds = config.HostileContactCooldownSeconds,
            InfectedDamageMultiplier = config.InfectedDamageMultiplier,
            ObjectiveLootValue = config.ObjectiveLootValue,
            AttackChargeRate = config.AttackChargeRate,
            AttackCooldownSeconds = config.AttackCooldownSeconds,
            AttackBaseDamage = config.AttackBaseDamage,
            AttackCriticalMultiplier = config.AttackCriticalMultiplier,
            AttackRange = config.AttackRange,
            AttackHalfFovDegrees = config.AttackHalfFovDegrees,
            AttackCriticalWindowMin = config.AttackCriticalWindowMin,
            AttackCriticalWindowMax = config.AttackCriticalWindowMax,
            AttackCriticalWindowSize = config.AttackCriticalWindowSize,
            AttackNoise = config.AttackNoise,
            AttackCriticalNoise = config.AttackCriticalNoise,
            MinModulesPerSector = Math.Max(40, config.MinModulesPerSector),
            MaxModulesPerSector = Math.Max(60, config.MaxModulesPerSector)
        };
    }
}

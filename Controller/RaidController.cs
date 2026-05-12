using System.Numerics;
using EFP.App;
using EFP.Platform;
using Silk.NET.Input;
using RaidModel = EFP.Model.Raid.Raid;

namespace EFP.Controller;

public sealed class RaidController
{
    private readonly DebugSettings _debugSettings;
    private readonly InputService _input;
    private readonly float _rotationSensitivity;
    private bool _attackHeld;
    private Vector2 _movementInput;
    private bool _queuedInteract;
    private bool _queuedUseMedkit;
    private bool _quietMovement;

    public RaidController(InputService input, DebugSettings debugSettings, float rotationSensitivity)
    {
        _input = input;
        _debugSettings = debugSettings;
        _rotationSensitivity = rotationSensitivity;
    }

    public bool MenuRequested { get; private set; }
    public bool RebuildRequested { get; private set; }

    public void PollInput(RaidModel raid)
    {
        MenuRequested = false;
        RebuildRequested = false;

        raid.IgnoreCollision = _debugSettings.IgnoreCollisions;

        var captureKeyboard = _debugSettings.CaptureKeyboard;
        var captureMouse = _debugSettings.CaptureMouse;

        if (!captureKeyboard && _input.IsKeyPressed(Key.Escape))
        {
            MenuRequested = true;
            return;
        }

        if (!captureKeyboard && _input.IsKeyPressed(Key.R))
        {
            RebuildRequested = true;
            return;
        }

        _movementInput = captureKeyboard ? Vector2.Zero : ReadMovementInput();
        _quietMovement = !captureKeyboard && (_input.IsKeyDown(Key.ShiftLeft) || _input.IsKeyDown(Key.ShiftRight));

        if (!captureKeyboard && _input.IsKeyPressed(Key.E)) _queuedInteract = true;
        if (!captureKeyboard && _input.IsKeyPressed(Key.Q)) _queuedUseMedkit = true;
        _attackHeld = !captureMouse && _input.IsMouseDown(MouseButton.Left);

        if (!captureMouse && _input.IsMouseDown(MouseButton.Right))
        {
            var rotationSpeed = _rotationSensitivity * _debugSettings.RotationSpeedMultiplier;
            raid.Player.RotateYaw(_input.MouseDelta.X * rotationSpeed);
        }
    }

    public void Tick(RaidModel raid, float fixedDeltaTime)
    {
        var interact = _queuedInteract;
        var useMedkit = _queuedUseMedkit;
        _queuedInteract = false;
        _queuedUseMedkit = false;

        raid.Tick(
            fixedDeltaTime,
            _movementInput,
            interact,
            _debugSettings.AllowCriticalMutation,
            _quietMovement,
            useMedkit,
            _attackHeld);
    }

    private Vector2 ReadMovementInput()
    {
        var movement = Vector2.Zero;
        if (_input.IsKeyDown(Key.W)) movement.Y -= 1f;
        if (_input.IsKeyDown(Key.S)) movement.Y += 1f;
        if (_input.IsKeyDown(Key.A)) movement.X -= 1f;
        if (_input.IsKeyDown(Key.D)) movement.X += 1f;
        return movement;
    }
}

using System.Numerics;
using Silk.NET.Input;

namespace EFP.Platform;

public sealed class InputService : IDisposable
{
    private readonly IInputContext _inputContext;
    private readonly HashSet<Key> _keysDown = [];
    private readonly HashSet<Key> _keysPressed = [];
    private readonly HashSet<MouseButton> _mouseButtonsDown = [];
    private readonly HashSet<MouseButton> _mouseButtonsPressed = [];
    private bool _hasMousePosition;

    private IKeyboard? _keyboard;
    private IMouse? _mouse;

    public InputService(IInputContext inputContext)
    {
        _inputContext = inputContext;
        InitializeDevices();
    }

    public Vector2 MousePosition { get; private set; }
    public Vector2 MouseDelta { get; private set; }

    public void Dispose()
    {
        if (_keyboard is not null)
        {
            _keyboard.KeyDown -= OnKeyDown;
            _keyboard.KeyUp -= OnKeyUp;
        }

        if (_mouse is not null)
        {
            _mouse.MouseDown -= OnMouseDown;
            _mouse.MouseUp -= OnMouseUp;
            _mouse.MouseMove -= OnMouseMove;
        }

        _inputContext.Dispose();
    }

    public void BeginFrame()
    {
    }

    public void EndFrame()
    {
        _keysPressed.Clear();
        _mouseButtonsPressed.Clear();
        MouseDelta = Vector2.Zero;
    }

    public bool IsKeyDown(Key key)
    {
        return _keysDown.Contains(key);
    }

    public bool IsKeyPressed(Key key)
    {
        return _keysPressed.Contains(key);
    }

    public bool IsMouseDown(MouseButton button)
    {
        return _mouseButtonsDown.Contains(button);
    }

    public bool IsMousePressed(MouseButton button)
    {
        return _mouseButtonsPressed.Contains(button);
    }

    private void InitializeDevices()
    {
        _keyboard = _inputContext.Keyboards.FirstOrDefault();
        if (_keyboard is not null)
        {
            _keyboard.KeyDown += OnKeyDown;
            _keyboard.KeyUp += OnKeyUp;
        }

        _mouse = _inputContext.Mice.FirstOrDefault();
        if (_mouse is not null)
        {
            _mouse.MouseDown += OnMouseDown;
            _mouse.MouseUp += OnMouseUp;
            _mouse.MouseMove += OnMouseMove;
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (_keysDown.Add(key)) _keysPressed.Add(key);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        _keysDown.Remove(key);
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (_mouseButtonsDown.Add(button)) _mouseButtonsPressed.Add(button);
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        _mouseButtonsDown.Remove(button);
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (!_hasMousePosition)
        {
            MousePosition = position;
            _hasMousePosition = true;
            return;
        }

        MouseDelta += position - MousePosition;
        MousePosition = position;
    }
}
using System.Numerics;
using Silk.NET.Input;

namespace EFP.Input;

public sealed class InputService
{
    private readonly IKeyboard? _keyboard;
    private readonly IMouse? _mouse;
    private readonly HashSet<Key> _currentKeys = [];
    private readonly HashSet<Key> _previousKeys = [];
    private readonly HashSet<MouseButton> _currentButtons = [];
    private readonly HashSet<MouseButton> _previousButtons = [];

    private Vector2 _mousePosition;
    private Vector2 _previousMousePosition;
    private Vector2 _scrollDelta;

    public InputService(IInputContext inputContext)
    {
        _keyboard = inputContext.Keyboards.FirstOrDefault();
        _mouse = inputContext.Mice.FirstOrDefault();

        if (_keyboard is not null)
        {
            _keyboard.KeyDown += OnKeyDown;
            _keyboard.KeyUp += OnKeyUp;
        }

        if (_mouse is not null)
        {
            _mouse.MouseDown += OnMouseDown;
            _mouse.MouseUp += OnMouseUp;
            _mouse.MouseMove += OnMouseMove;
            _mouse.Scroll += OnMouseScroll;
            _mousePosition = _mouse.Position;
            _previousMousePosition = _mouse.Position;
        }
    }

    public Vector2 MousePosition => _mousePosition;
    public Vector2 MouseDelta { get; private set; }
    public Vector2 ScrollDelta => _scrollDelta;

    public void BeginFrame()
    {
        MouseDelta = _mousePosition - _previousMousePosition;
    }

    public void EndFrame()
    {
        _previousKeys.Clear();
        foreach (var key in _currentKeys)
        {
            _previousKeys.Add(key);
        }

        _previousButtons.Clear();
        foreach (var button in _currentButtons)
        {
            _previousButtons.Add(button);
        }

        _previousMousePosition = _mousePosition;
        _scrollDelta = Vector2.Zero;
        MouseDelta = Vector2.Zero;
    }

    public bool IsKeyDown(Key key) => _currentKeys.Contains(key);

    public bool IsKeyPressed(Key key) => _currentKeys.Contains(key) && !_previousKeys.Contains(key);

    public bool IsMouseDown(MouseButton button) => _currentButtons.Contains(button);

    public bool IsMousePressed(MouseButton button) =>
        _currentButtons.Contains(button) && !_previousButtons.Contains(button);

    private void OnKeyDown(IKeyboard keyboard, Key key, int scanCode)
    {
        _currentKeys.Add(key);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scanCode)
    {
        _currentKeys.Remove(key);
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        _currentButtons.Add(button);
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        _currentButtons.Remove(button);
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        _mousePosition = position;
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
    {
        _scrollDelta += new Vector2(wheel.X, wheel.Y);
    }
}
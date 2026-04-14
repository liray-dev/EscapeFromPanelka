using System.Numerics;
using EFP.GameState;
using EFP.Input;
using EFP.Rendering;
using EFP.Utilities;
using Silk.NET.Input;

namespace EFP.UI.Screens;

public abstract class Screen(IGameStateContext context)
{
    protected IGameStateContext Context { get; } = context;
    private InputService Input => Context.Input;
    private UiRenderer Ui => Context.UiRenderer;

    public virtual void OnOpen()
    {
    }

    public virtual void OnClose()
    {
    }

    public virtual void Update(double deltaTime)
    {
    }

    public virtual void FixedUpdate(double fixedDeltaTime)
    {
    }

    public abstract void Draw();

    protected void Fill(RectF rect, ColorRgba color)
    {
        Ui.DrawRect(rect, color);
    }

    protected void DrawOutline(RectF rect, float thickness, ColorRgba color)
    {
        Ui.DrawOutline(rect, thickness, color);
    }

    protected void DrawText(Vector2 position, string text, float scale, ColorRgba color)
    {
        Ui.DrawText(text, position, scale, color);
    }

    protected void DrawCenteredText(RectF rect, string text, float scale, ColorRgba color)
    {
        var size = Ui.MeasureText(text, scale);
        var pos = new Vector2(
            rect.X + (rect.Width - size.X) * 0.5f,
            rect.Y + (rect.Height - size.Y) * 0.5f);
        Ui.DrawText(text, pos, scale, color);
    }

    protected void DrawTexture(Texture2D texture, RectF rect, ColorRgba tint)
    {
        Ui.DrawTexture(texture, rect, tint);
    }

    protected bool Button(RectF rect, string text)
    {
        var hovered = rect.Contains(Input.MousePosition);
        var background = hovered
            ? ColorRgba.FromBytes(66, 86, 104, 232)
            : ColorRgba.FromBytes(42, 53, 66, 228);

        var border = hovered
            ? ColorRgba.FromBytes(190, 215, 255)
            : ColorRgba.FromBytes(118, 134, 151);

        Fill(rect, background);
        DrawOutline(rect, 2f, border);
        DrawCenteredText(rect, text, 0.60f, ColorRgba.White);

        return hovered && Input.IsMousePressed(MouseButton.Left);
    }
}
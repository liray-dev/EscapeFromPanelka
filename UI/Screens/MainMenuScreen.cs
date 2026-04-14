using System.Numerics;
using EFP.GameState;
using EFP.Utilities;
using Silk.NET.Input;

namespace EFP.UI.Screens;

public sealed class MainMenuScreen(IGameStateContext context, Action onStart, Action onExit) : Screen(context)
{
    public override void Update(double deltaTime)
    {
        if (Context.DebugSettings.CaptureKeyboard) return;

        if (Context.Input.IsKeyPressed(Key.Enter)) onStart();

        if (Context.Input.IsKeyPressed(Key.Escape)) onExit();
    }

    public override void Draw()
    {
        var width = Context.Window.Size.X;
        var height = Context.Window.Size.Y;

        Fill(new RectF(0f, 0f, width, height), ColorRgba.FromBytes(11, 14, 18));
        Fill(new RectF(0f, height * 0.56f, width, height * 0.44f), ColorRgba.FromBytes(22, 26, 31));

        var card = new RectF(width * 0.5f - 290f, height * 0.5f - 185f, 580f, 370f);
        Fill(card, ColorRgba.FromBytes(21, 27, 34, 242));
        DrawOutline(card, 2f, ColorRgba.FromBytes(102, 118, 136));

        DrawText(new Vector2(card.X + 28f, card.Y + 28f), "Escape From Panelka", 0.95f,
            ColorRgba.FromBytes(236, 239, 244));

        var startButton = new RectF(card.X + 28f, card.Bottom - 112f, card.Width - 56f, 42f);
        var exitButton = new RectF(card.X + 28f, card.Bottom - 58f, card.Width - 56f, 36f);

        if (Button(startButton, "Запуск")) onStart();

        if (Button(exitButton, "Выход")) onExit();

        DrawText(new Vector2(card.X + 28f, card.Bottom - 148f), "Enter — запуск   Escape — выход   F1 — debug", 0.34f,
            ColorRgba.FromBytes(138, 147, 158));
    }
}
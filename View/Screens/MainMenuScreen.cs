using System.Numerics;
using EFP.GameState;
using EFP.Utilities;
using Silk.NET.Input;

namespace EFP.View.Screens;

public sealed class MainMenuScreen(IGameStateContext context, Action onEnterLobby, Action onExit) : Screen(context)
{
    private string _stubMessage = string.Empty;
    private float _stubTimer;

    public override void Update(double deltaTime)
    {
        if (_stubTimer > 0f) _stubTimer = MathF.Max(0f, _stubTimer - (float)deltaTime);

        if (Context.DebugSettings.CaptureKeyboard) return;
        if (Context.Input.IsKeyPressed(Key.Enter)) onEnterLobby();
        if (Context.Input.IsKeyPressed(Key.Escape)) onExit();
    }

    public override void Draw()
    {
        var width = Context.Window.Size.X;
        var height = Context.Window.Size.Y;

        DrawBackdrop(width, height);
        DrawTitle();
        DrawButtons(height);
        DrawProfilePanel(width);
        DrawVersionBanner(height);
        DrawNewsCard(width, height);

        if (_stubTimer > 0f && !string.IsNullOrEmpty(_stubMessage))
        {
            var rect = new RectF((width - 420f) * 0.5f, height * 0.5f - 22f, 420f, 44f);
            Fill(rect, ColorRgba.FromBytes(18, 22, 28, 232));
            DrawOutline(rect, 2f, ColorRgba.FromBytes(192, 168, 96));
            DrawCenteredText(rect, _stubMessage, 0.40f, ColorRgba.FromBytes(232, 218, 168));
        }
    }

    private void DrawBackdrop(int width, int height)
    {
        Fill(new RectF(0f, 0f, width, height), ColorRgba.FromBytes(11, 14, 18));
        Fill(new RectF(0f, height * 0.66f, width, height * 0.34f), ColorRgba.FromBytes(18, 22, 28));

        var leftPanel = new RectF(0f, 0f, width * 0.38f, height);
        Fill(leftPanel, ColorRgba.FromBytes(8, 10, 13, 178));
    }

    private void DrawTitle()
    {
        DrawText(new Vector2(56f, 88f), "ESCAPE FROM PANELKA", 1.10f, ColorRgba.FromBytes(236, 240, 246));
        DrawText(new Vector2(56f, 142f), "Pre-Alpha · вход в безопасный блок", 0.34f,
            ColorRgba.FromBytes(160, 170, 184));
    }

    private void DrawButtons(int windowHeight)
    {
        var top = windowHeight * 0.32f;
        var labels = new[] { "ОТПРАВИТЬСЯ В РЕЙД", "ПЕРСОНАЖ", "ТОРГОВЛЯ", "НАСТРОЙКИ", "ВЫЙТИ" };
        Action[] actions =
        [
            onEnterLobby,
            () => ShowStub("Раздел «Персонаж» появится в следующей сборке"),
            () => ShowStub("Раздел «Торговля» появится в следующей сборке"),
            () => ShowStub("Раздел «Настройки» появится в следующей сборке"),
            onExit
        ];

        for (var i = 0; i < labels.Length; i++)
        {
            var rect = new RectF(56f, top + i * 56f, 320f, 44f);
            if (MenuButton(rect, labels[i])) actions[i]();
        }
    }

    private void DrawProfilePanel(int windowWidth)
    {
        var profile = Context.Profile;
        var panel = new RectF(windowWidth - 320f, 56f, 280f, 132f);
        Fill(panel, ColorRgba.FromBytes(18, 22, 28, 218));
        DrawOutline(panel, 2f, ColorRgba.FromBytes(96, 110, 124));

        DrawText(new Vector2(panel.X + 22f, panel.Y + 18f), profile.Name, 0.50f,
            ColorRgba.FromBytes(232, 236, 244));
        DrawText(new Vector2(panel.X + 22f, panel.Y + 56f), $"УРОВЕНЬ ЧВК {profile.Level}", 0.40f,
            ColorRgba.FromBytes(180, 196, 212));
        DrawText(new Vector2(panel.X + 22f, panel.Y + 90f), $"₽ {FormatCurrency(profile.Currency)}", 0.42f,
            ColorRgba.FromBytes(232, 200, 120));
    }

    private void DrawVersionBanner(int windowHeight)
    {
        var rect = new RectF(48f, windowHeight - 96f, 540f, 70f);
        Fill(rect, ColorRgba.FromBytes(18, 22, 28, 196));
        DrawOutline(rect, 1f, ColorRgba.FromBytes(96, 110, 124));
        DrawText(new Vector2(rect.X + 16f, rect.Y + 10f),
            "Pre-Alpha version 0.04.00", 0.34f, ColorRgba.FromBytes(214, 224, 236));
        DrawText(new Vector2(rect.X + 16f, rect.Y + 32f),
            "Эта версия предназначена для тестирования и не отражает финального качества.", 0.30f,
            ColorRgba.FromBytes(160, 170, 184));
        DrawText(new Vector2(rect.X + 16f, rect.Y + 50f),
            "Удачи в рейде.", 0.30f, ColorRgba.FromBytes(160, 170, 184));
    }

    private void DrawNewsCard(int windowWidth, int windowHeight)
    {
        var rect = new RectF(windowWidth - 360f, windowHeight - 200f, 320f, 168f);
        Fill(rect, ColorRgba.FromBytes(20, 26, 32, 232));
        DrawOutline(rect, 2f, ColorRgba.FromBytes(96, 110, 124));
        DrawText(new Vector2(rect.X + 18f, rect.Y + 16f), "ЛЕНТА СЕКТОРА", 0.38f,
            ColorRgba.FromBytes(214, 224, 236));
        DrawText(new Vector2(rect.X + 18f, rect.Y + 56f),
            "«Война это дело такое, тут либо ты,", 0.32f, ColorRgba.FromBytes(180, 196, 212));
        DrawText(new Vector2(rect.X + 18f, rect.Y + 78f),
            " либо тебя.»", 0.32f, ColorRgba.FromBytes(180, 196, 212));
        DrawText(new Vector2(rect.X + 18f, rect.Y + 118f),
            "Цени жизнь и оберегай спокойствие.", 0.30f, ColorRgba.FromBytes(160, 170, 184));
    }

    private bool MenuButton(RectF rect, string text)
    {
        var hovered = !Context.DebugSettings.CaptureMouse && rect.Contains(Context.Input.MousePosition);
        var background = hovered
            ? ColorRgba.FromBytes(42, 56, 70, 232)
            : ColorRgba.FromBytes(20, 26, 32, 196);
        var border = hovered
            ? ColorRgba.FromBytes(214, 224, 236)
            : ColorRgba.FromBytes(86, 100, 116);
        var textColor = hovered ? ColorRgba.White : ColorRgba.FromBytes(214, 224, 236);

        Fill(rect, background);
        DrawOutline(rect, 1f, border);
        DrawText(new Vector2(rect.X + 18f, rect.Y + 12f), text, 0.46f, textColor);

        return hovered && Context.Input.IsMousePressed(MouseButton.Left);
    }

    private void ShowStub(string message)
    {
        _stubMessage = message;
        _stubTimer = 2.0f;
    }

    private static string FormatCurrency(long value)
    {
        return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
    }
}

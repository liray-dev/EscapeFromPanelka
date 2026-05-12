using System.Numerics;
using EFP.GameState;
using EFP.Model.Inventory;
using EFP.Utilities;
using Silk.NET.Input;

namespace EFP.View.Screens;

public sealed class LobbyScreen(IGameStateContext context, Action onEnterRaid, Action onBack) : Screen(context)
{
    private const float CellSize = 48f;
    private const float CellGap = 2f;

    public override void Update(double deltaTime)
    {
        if (Context.DebugSettings.CaptureKeyboard) return;
        if (Context.Input.IsKeyPressed(Key.Escape)) onBack();
        if (Context.Input.IsKeyPressed(Key.Enter)) onEnterRaid();
    }

    public override void Draw()
    {
        var profile = Context.Profile;
        var width = Context.Window.Size.X;
        var height = Context.Window.Size.Y;

        Fill(new RectF(0f, 0f, width, height), ColorRgba.FromBytes(11, 14, 18));

        DrawText(new Vector2(48f, 38f), "БЕЗОПАСНЫЙ БЛОК", 0.78f, ColorRgba.FromBytes(232, 236, 244));
        DrawText(new Vector2(48f, 82f), "Подготовь снаряжение и отправляйся в рейд", 0.36f,
            ColorRgba.FromBytes(160, 174, 188));

        DrawProfilePanel(width, profile);
        DrawStash(profile.Stash);
        DrawButtons(width, height);

        DrawText(new Vector2(48f, height - 36f),
            "Enter — В рейд   Escape — В меню", 0.34f, ColorRgba.FromBytes(140, 152, 168));
    }

    private void DrawProfilePanel(int windowWidth, Model.Profile.PlayerProfile profile)
    {
        var panel = new RectF(windowWidth - 360f, 32f, 320f, 132f);
        Fill(panel, ColorRgba.FromBytes(20, 26, 32, 232));
        DrawOutline(panel, 2f, ColorRgba.FromBytes(96, 110, 124));

        DrawText(new Vector2(panel.X + 22f, panel.Y + 18f), profile.Name, 0.56f,
            ColorRgba.FromBytes(232, 236, 244));
        DrawText(new Vector2(panel.X + 22f, panel.Y + 56f), $"УРОВЕНЬ ЧВК {profile.Level}", 0.40f,
            ColorRgba.FromBytes(180, 196, 212));
        DrawText(new Vector2(panel.X + 22f, panel.Y + 88f), $"₽ {FormatCurrency(profile.Currency)}", 0.46f,
            ColorRgba.FromBytes(232, 200, 120));
    }

    private void DrawStash(InventoryGrid stash)
    {
        const float originX = 48f;
        const float originY = 168f;
        var totalWidth = stash.Width * CellSize + (stash.Width + 1) * CellGap;
        var totalHeight = stash.Height * CellSize + (stash.Height + 1) * CellGap;

        DrawText(new Vector2(originX, originY - 28f),
            $"СТЭШ — {stash.OccupiedCells}/{stash.TotalCells}   Стоимость: ₽ {stash.TotalValue}",
            0.42f, ColorRgba.FromBytes(214, 224, 236));

        Fill(new RectF(originX - CellGap, originY - CellGap, totalWidth, totalHeight),
            ColorRgba.FromBytes(18, 22, 28, 232));
        DrawOutline(new RectF(originX - CellGap, originY - CellGap, totalWidth, totalHeight),
            2f, ColorRgba.FromBytes(88, 104, 120));

        for (var y = 0; y < stash.Height; y++)
        {
            for (var x = 0; x < stash.Width; x++)
            {
                var cell = new RectF(
                    originX + x * (CellSize + CellGap),
                    originY + y * (CellSize + CellGap),
                    CellSize,
                    CellSize);
                Fill(cell, ColorRgba.FromBytes(26, 32, 38, 230));
                DrawOutline(cell, 1f, ColorRgba.FromBytes(54, 64, 74));
            }
        }

        foreach (var slot in stash.Slots)
        {
            var rect = new RectF(
                originX + slot.X * (CellSize + CellGap),
                originY + slot.Y * (CellSize + CellGap),
                slot.Item.Width * CellSize + (slot.Item.Width - 1) * CellGap,
                slot.Item.Height * CellSize + (slot.Item.Height - 1) * CellGap);

            var color = new ColorRgba(
                slot.Item.Color.Length > 0 ? slot.Item.Color[0] : 0.6f,
                slot.Item.Color.Length > 1 ? slot.Item.Color[1] : 0.6f,
                slot.Item.Color.Length > 2 ? slot.Item.Color[2] : 0.6f,
                slot.Item.Color.Length > 3 ? slot.Item.Color[3] : 1f);
            Fill(rect, color);
            DrawOutline(rect, 2f, ColorRgba.FromBytes(232, 236, 244));
            DrawText(new Vector2(rect.X + 6f, rect.Y + 6f), slot.Item.Label, 0.28f,
                ColorRgba.FromBytes(18, 22, 28));
        }
    }

    private void DrawButtons(int windowWidth, int windowHeight)
    {
        var raidButton = new RectF(windowWidth - 360f, windowHeight - 180f, 320f, 60f);
        if (Button(raidButton, "В РЕЙД")) onEnterRaid();

        var backButton = new RectF(windowWidth - 360f, windowHeight - 108f, 320f, 44f);
        if (Button(backButton, "В МЕНЮ")) onBack();
    }

    private static string FormatCurrency(long value)
    {
        return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
    }
}

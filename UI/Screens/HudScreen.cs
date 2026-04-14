using System.Numerics;
using EFP.GameState;
using EFP.Utilities;

namespace EFP.UI.Screens;

public sealed class HudScreen(IGameStateContext context) : Screen(context)
{
    public void Draw(string stateName, World.World world, FrameStats frameStats)
    {
        var panel = new RectF(16f, 16f, 390f, 154f);
        Fill(panel, ColorRgba.FromBytes(18, 22, 28, 216));
        DrawOutline(panel, 2f, ColorRgba.FromBytes(88, 104, 120, 255));

        DrawText(new Vector2(30f, 28f), "HUD / Sector Readout", 0.44f, ColorRgba.FromBytes(236, 239, 244));
        DrawText(new Vector2(30f, 54f), $"State: {stateName}", 0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 72f),
            $"Position: {world.Player.Transform.Position.X:0.00}  {world.Player.Transform.Position.Y:0.00}  {world.Player.Transform.Position.Z:0.00}",
            0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 90f), $"Rotation Y: {world.Player.YawDegrees:0.00}°", 0.34f,
            ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 108f),
            $"Seed: {world.Seed}   Modules: {world.ModuleCount}   Props: {world.PropCount}", 0.34f,
            ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 126f),
            $"FPS: {frameStats.FramesPerSecond:0.0}   Frame: {frameStats.FrameTimeMs:0.00} ms", 0.34f,
            ColorRgba.FromBytes(187, 196, 205));

        var hint = new RectF(16f, Context.Window.Size.Y - 52f, 560f, 36f);
        Fill(hint, ColorRgba.FromBytes(18, 22, 28, 196));
        DrawOutline(hint, 2f, ColorRgba.FromBytes(88, 104, 120, 255));
        DrawText(new Vector2(28f, Context.Window.Size.Y - 41f),
            "WASD — движение   ПКМ + мышь — поворот   R — пересобрать сектор   Escape — в меню", 0.34f,
            ColorRgba.FromBytes(214, 220, 227));
    }

    public override void Draw()
    {
    }
}
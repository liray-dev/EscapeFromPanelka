using System.Numerics;
using EFP.Entities;
using EFP.GameState;
using EFP.Utilities;

namespace EFP.UI.Screens;

public sealed class HudScreen(IGameStateContext context) : Screen(context)
{
    public void Draw(string stateName, PlayerCube player, FrameStats frameStats)
    {
        var panel = new RectF(16f, 16f, 350f, 132f);
        Fill(panel, ColorRgba.FromBytes(18, 22, 28, 216));
        DrawOutline(panel, 2f, ColorRgba.FromBytes(88, 104, 120));

        DrawText(new Vector2(30f, 28f), "HUD / Prototype Readout", 0.44f, ColorRgba.FromBytes(236, 239, 244));
        DrawText(new Vector2(30f, 54f), $"State: {stateName}", 0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 72f),
            $"Position: {player.Transform.Position.X:0.00}  {player.Transform.Position.Y:0.00}  {player.Transform.Position.Z:0.00}",
            0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 90f), $"Rotation Y: {player.YawDegrees:0.00}°", 0.34f,
            ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 108f),
            $"FPS: {frameStats.FramesPerSecond:0.0}   Frame: {frameStats.FrameTimeMs:0.00} ms", 0.34f,
            ColorRgba.FromBytes(187, 196, 205));

        var hint = new RectF(16f, Context.Window.Size.Y - 52f, 460f, 36f);
        Fill(hint, ColorRgba.FromBytes(18, 22, 28, 196));
        DrawOutline(hint, 2f, ColorRgba.FromBytes(88, 104, 120));
    }

    public override void Draw()
    {
    }
}
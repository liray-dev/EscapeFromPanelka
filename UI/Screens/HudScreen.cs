using System.Numerics;
using EFP.GameState;
using EFP.Utilities;
using EFP.World;

namespace EFP.UI.Screens;

public sealed class HudScreen(IGameStateContext context) : Screen(context)
{
    public void Draw(string stateName, World.World world, FrameStats frameStats)
    {
        var panel = new RectF(16f, 16f, 520f, 206f);
        Fill(panel, ColorRgba.FromBytes(18, 22, 28, 216));
        DrawOutline(panel, 2f, ColorRgba.FromBytes(88, 104, 120, 255));

        DrawText(new Vector2(30f, 28f), "HUD / Raid Readout", 0.44f, ColorRgba.FromBytes(236, 239, 244));
        DrawText(new Vector2(30f, 52f), $"State: {stateName}", 0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 70f), $"Position: {world.Player.Transform.Position.X:0.00}  {world.Player.Transform.Position.Y:0.00}  {world.Player.Transform.Position.Z:0.00}", 0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 88f), $"Rotation Y: {world.Player.YawDegrees:0.00}°", 0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 106f), $"Seed: {world.Seed}   Modules: {world.ModuleCount}   Props: {world.PropCount}", 0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 124f), $"Blocked passages: {world.LockedPassageCount}   Power: {(world.PowerRestored ? "restored" : "offline")}", 0.34f, world.PowerRestored ? ColorRgba.FromBytes(168, 204, 176, 255) : ColorRgba.FromBytes(231, 179, 96, 255));
        DrawText(new Vector2(30f, 142f), $"Objective: {world.ObjectiveLabel}", 0.34f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(30f, 160f), $"Pressure: {world.PressureLabel}   Time: {world.TimeRemainingSeconds:0.0}s", 0.34f, GetPressureColor(world));
        DrawText(new Vector2(30f, 178f), $"FPS: {frameStats.FramesPerSecond:0.0}   Frame: {frameStats.FrameTimeMs:0.00} ms", 0.34f, ColorRgba.FromBytes(187, 196, 205));

        var hint = new RectF(16f, Context.Window.Size.Y - 52f, 660f, 36f);
        Fill(hint, ColorRgba.FromBytes(18, 22, 28, 196));
        DrawOutline(hint, 2f, ColorRgba.FromBytes(88, 104, 120, 255));
        DrawText(new Vector2(28f, Context.Window.Size.Y - 41f), "WASD — движение   ПКМ + мышь — поворот   E — взаимодействие   R — новый сектор   Escape — в меню", 0.34f, ColorRgba.FromBytes(214, 220, 227));

        if (!string.IsNullOrWhiteSpace(world.ContextHint))
        {
            var contextBox = new RectF(16f, Context.Window.Size.Y - 140f, 520f, 34f);
            Fill(contextBox, ColorRgba.FromBytes(48, 36, 26, 214));
            DrawOutline(contextBox, 2f, ColorRgba.FromBytes(170, 124, 74, 255));
            DrawText(new Vector2(28f, Context.Window.Size.Y - 129f), world.ContextHint, 0.34f, ColorRgba.FromBytes(241, 226, 204, 255));
        }

        if (world.CanInteract)
        {
            var interactBox = new RectF(16f, Context.Window.Size.Y - 96f, 520f, 34f);
            Fill(interactBox, ColorRgba.FromBytes(28, 38, 44, 218));
            DrawOutline(interactBox, 2f, ColorRgba.FromBytes(108, 164, 160, 255));
            DrawText(new Vector2(28f, Context.Window.Size.Y - 85f), world.InteractionPrompt, 0.36f, ColorRgba.FromBytes(231, 241, 241));
        }
    }

    public override void Draw()
    {
    }

    private static ColorRgba GetPressureColor(World.World world)
    {
        return world.Phase switch
        {
            RaidPhase.Extracted => ColorRgba.FromBytes(225, 214, 120, 255),
            RaidPhase.Failed => ColorRgba.FromBytes(232, 122, 122, 255),
            _ => world.PressureLevel switch
            {
                RaidPressureLevel.Stable => ColorRgba.FromBytes(178, 202, 184, 255),
                RaidPressureLevel.Pressure => ColorRgba.FromBytes(228, 191, 109, 255),
                RaidPressureLevel.Critical => ColorRgba.FromBytes(230, 122, 102, 255),
                _ => ColorRgba.FromBytes(187, 196, 205, 255)
            }
        };
    }
}

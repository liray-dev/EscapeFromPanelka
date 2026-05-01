using System.Numerics;
using EFP.GameState;
using EFP.Utilities;
using EFP.World;

namespace EFP.UI.Screens;

public sealed class HudScreen(IGameStateContext context) : Screen(context)
{
    public void Draw(string stateName, World.World world, FrameStats frameStats)
    {
        DrawLeftPanel(stateName, world, frameStats);
        DrawBottomBar(world);
        DrawContextPanels(world);

        if (world.IsRaidResolved) DrawSummary(world);
    }

    public override void Draw()
    {
    }

    private void DrawLeftPanel(string stateName, World.World world, FrameStats frameStats)
    {
        var panel = new RectF(16f, 16f, 690f, 332f);
        Fill(panel, ColorRgba.FromBytes(18, 22, 28, 216));
        DrawOutline(panel, 2f, ColorRgba.FromBytes(88, 104, 120));

        DrawText(new Vector2(30f, 28f), "HUD / Raid Readout", 0.44f, ColorRgba.FromBytes(236, 239, 244));
        DrawText(new Vector2(30f, 52f), $"State: {stateName}", 0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 70f),
            $"Position: {world.Player.Transform.Position.X:0.00}  {world.Player.Transform.Position.Y:0.00}  {world.Player.Transform.Position.Z:0.00}",
            0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 88f),
            $"Rotation Y: {world.Player.YawDegrees:0.00}°   Move scale: {world.PlayerMoveScale:0.00}   Quiet walk: {(world.QuietMovementActive ? "on" : "off")}",
            0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 106f),
            $"Seed: {world.Seed}   Modules: {world.ModuleCount}   Props: {world.PropCount}   Room x{world.RoomSizeMultiplier:0.00}",
            0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 124f),
            $"Doors: {world.DoorCount}   Blocked: {world.LockedPassageCount}   Power: {(world.PowerRestored ? "restored" : "offline")}",
            0.34f, world.PowerRestored ? ColorRgba.FromBytes(168, 204, 176) : ColorRgba.FromBytes(231, 179, 96));
        DrawText(new Vector2(30f, 142f),
            $"Lights: {world.LightCount}   Infected: {world.ActiveInfectedZoneCount}   Alert: {world.AlertedHostileCount}   Hunt: {world.HuntingHostileCount}",
            0.34f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(30f, 160f),
            $"Cargo: {world.CargoCount} items   Value: {world.CargoValue}   Medkits: {world.MedkitCount}",
            0.34f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(30f, 178f), $"Objective: {world.ObjectiveLabel}", 0.34f,
            ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(30f, 196f), $"Pressure: {world.PressureLabel}   Time: {world.TimeRemainingSeconds:0.0}s",
            0.34f, GetPressureColor(world));
        DrawText(new Vector2(30f, 214f),
            $"Mutation: {(world.CriticalMutationActive ? "active" : "dormant")}   Ignore collision: {(world.IgnoreCollision ? "on" : "off")}",
            0.34f,
            world.CriticalMutationActive ? ColorRgba.FromBytes(212, 126, 151) : ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 232f),
            $"Noise: {world.PlayerNoiseLevel:0.00}   Visibility: {world.PlayerVisibilityLevel:0.00}   HP: {world.PlayerHealth:0}/{Context.Config.Gameplay.PlayerMaxHealth:0}",
            0.34f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(30f, 250f),
            $"FPS: {frameStats.FramesPerSecond:0.0}   Frame: {frameStats.FrameTimeMs:0.00} ms", 0.34f,
            ColorRgba.FromBytes(187, 196, 205));

        DrawMeter(new Vector2(30f, 286f), 190f, 12f, world.PlayerHealthNormalized,
            ColorRgba.FromBytes(46, 56, 64),
            world.PlayerHealthNormalized > 0.45f
                ? ColorRgba.FromBytes(112, 191, 128)
                : ColorRgba.FromBytes(212, 92, 92),
            "Health");
        DrawMeter(new Vector2(250f, 286f), 160f, 12f, world.PlayerNoiseLevel,
            ColorRgba.FromBytes(46, 56, 64),
            ColorRgba.FromBytes(218, 164, 86),
            "Noise");
        DrawMeter(new Vector2(438f, 286f), 160f, 12f, world.PlayerVisibilityLevel,
            ColorRgba.FromBytes(46, 56, 64),
            ColorRgba.FromBytes(114, 178, 224),
            "Light");
    }

    private void DrawBottomBar(World.World world)
    {
        var hint = new RectF(16f, Context.Window.Size.Y - 52f, 1000f, 36f);
        Fill(hint, ColorRgba.FromBytes(18, 22, 28, 196));
        DrawOutline(hint, 2f, ColorRgba.FromBytes(88, 104, 120));
        DrawText(new Vector2(28f, Context.Window.Size.Y - 41f),
            "WASD — движение по yaw   Shift — тихий шаг   ПКМ + мышь — поворот   E — взаимодействие   Q — аптечка   R — новый сектор   F1 — debug   Escape — в меню",
            0.34f, ColorRgba.FromBytes(214, 220, 227));
    }

    private void DrawContextPanels(World.World world)
    {
        if (!string.IsNullOrWhiteSpace(world.ContextHint))
        {
            var contextBox = new RectF(16f, Context.Window.Size.Y - 140f, 680f, 34f);
            Fill(contextBox, ColorRgba.FromBytes(48, 36, 26, 214));
            DrawOutline(contextBox, 2f, ColorRgba.FromBytes(170, 124, 74));
            DrawText(new Vector2(28f, Context.Window.Size.Y - 129f), world.ContextHint, 0.34f,
                ColorRgba.FromBytes(241, 226, 204));
        }

        if (world.CanInteract)
        {
            var interactBox = new RectF(16f, Context.Window.Size.Y - 96f, 680f, 34f);
            Fill(interactBox, ColorRgba.FromBytes(28, 38, 44, 218));
            DrawOutline(interactBox, 2f, ColorRgba.FromBytes(108, 164, 160));
            DrawText(new Vector2(28f, Context.Window.Size.Y - 85f), world.InteractionPrompt, 0.36f,
                ColorRgba.FromBytes(231, 241, 241));
        }
    }

    private void DrawSummary(World.World world)
    {
        var width = 520f;
        var height = 252f;
        var rect = new RectF(
            (Context.Window.Size.X - width) * 0.5f,
            (Context.Window.Size.Y - height) * 0.5f,
            width,
            height);

        Fill(rect, ColorRgba.FromBytes(14, 17, 22, 236));
        DrawOutline(rect, 2f, world.Phase == RaidPhase.Extracted
            ? ColorRgba.FromBytes(190, 176, 108)
            : ColorRgba.FromBytes(174, 96, 96));

        var title = world.Phase == RaidPhase.Extracted ? "Raid Summary / Extracted" : "Raid Summary / Failed";
        DrawCenteredText(new RectF(rect.X, rect.Y + 20f, rect.Width, 32f), title, 0.52f,
            ColorRgba.FromBytes(236, 239, 244));

        DrawText(new Vector2(rect.X + 28f, rect.Y + 70f), $"Cargo recovered: {world.CargoCount}", 0.38f,
            ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 94f), $"Cargo value: {world.CargoValue}", 0.38f,
            ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 118f),
            $"Health left: {world.PlayerHealth:0}/{Context.Config.Gameplay.PlayerMaxHealth:0}", 0.38f,
            ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 142f), $"Medkits left: {world.MedkitCount}", 0.38f,
            ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 166f), $"Time spent: {world.ElapsedRaidSeconds:0.0}s", 0.38f,
            ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 190f),
            $"Pressure end state: {world.PressureLabel}", 0.38f, GetPressureColor(world));

        DrawCenteredText(new RectF(rect.X, rect.Bottom - 44f, rect.Width, 24f),
            "R — новый сектор   Escape — в меню", 0.38f, ColorRgba.FromBytes(187, 196, 205));
    }

    private void DrawMeter(Vector2 position, float width, float height, float value, ColorRgba background,
        ColorRgba fillColor, string label)
    {
        var clamped = Math.Clamp(value, 0f, 1f);
        var rect = new RectF(position.X, position.Y, width, height);
        Fill(rect, background);
        DrawOutline(rect, 1f, ColorRgba.FromBytes(86, 98, 112));
        if (clamped > 0.001f)
            Fill(new RectF(position.X + 1f, position.Y + 1f, (width - 2f) * clamped, height - 2f), fillColor);

        DrawText(new Vector2(position.X, position.Y - 16f), label, 0.30f, ColorRgba.FromBytes(187, 196, 205));
    }

    private static ColorRgba GetPressureColor(World.World world)
    {
        return world.Phase switch
        {
            RaidPhase.Extracted => ColorRgba.FromBytes(225, 214, 120),
            RaidPhase.Failed => ColorRgba.FromBytes(232, 122, 122),
            _ => world.PressureLevel switch
            {
                RaidPressureLevel.Stable => ColorRgba.FromBytes(178, 202, 184),
                RaidPressureLevel.Pressure => ColorRgba.FromBytes(228, 191, 109),
                RaidPressureLevel.Critical => ColorRgba.FromBytes(230, 122, 102),
                _ => ColorRgba.FromBytes(187, 196, 205)
            }
        };
    }
}
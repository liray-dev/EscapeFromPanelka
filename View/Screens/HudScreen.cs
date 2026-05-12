using System.Numerics;
using EFP.GameState;
using EFP.Model.Raid;
using EFP.Utilities;
using RaidModel = EFP.Model.Raid.Raid;

namespace EFP.View.Screens;

public sealed class HudScreen(IGameStateContext context) : Screen(context)
{
    private readonly RaidOutcomeScreen _outcomeScreen = new(context);

    public void Draw(string stateName, RaidModel raid, RaidViewModel viewModel, FrameStats frameStats)
    {
        DrawLeftPanel(stateName, raid, viewModel, frameStats);
        DrawBottomBar();
        DrawContextPanels(viewModel);
        DrawAttackMeter(raid, viewModel);
        DrawSearchOverlay(viewModel);
        DrawExtractionPanel(raid);

        if (viewModel.RaidFinished)
        {
            _outcomeScreen.Configure(raid, viewModel);
            _outcomeScreen.Draw();
        }
    }

    private void DrawExtractionPanel(RaidModel raid)
    {
        if (raid.ExtractionPoints.Count == 0) return;

        var height = 28f + raid.ExtractionPoints.Count * 38f;
        var panel = new RectF(16f, Context.Window.Size.Y - 200f - height, 360f, height);
        Fill(panel, ColorRgba.FromBytes(18, 22, 28, 222));
        DrawOutline(panel, 2f, ColorRgba.FromBytes(96, 110, 124));

        DrawText(new Vector2(panel.X + 14f, panel.Y + 8f), "РАДИАЛЬНЫЕ ВЫХОДЫ", 0.34f,
            ColorRgba.FromBytes(214, 224, 236));

        var playerXZ = new Vector2(raid.Player.Transform.Position.X, raid.Player.Transform.Position.Z);

        for (var i = 0; i < raid.ExtractionPoints.Count; i++)
        {
            var extract = raid.ExtractionPoints[i];
            var rowY = panel.Y + 28f + i * 38f;
            var done = extract.Condition.IsComplete(raid);
            var statusColor = done
                ? ColorRgba.FromBytes(168, 218, 154)
                : ColorRgba.FromBytes(232, 188, 110);

            var distance = Vector2.Distance(playerXZ,
                new Vector2(extract.Position.X, extract.Position.Z));
            var marker = extract.Used ? "✓" : done ? "●" : "○";

            DrawText(new Vector2(panel.X + 14f, rowY), $"{marker} {extract.Label}  · {distance:0}м", 0.34f, statusColor);
            DrawText(new Vector2(panel.X + 14f, rowY + 18f), extract.Condition.Describe(raid), 0.28f,
                ColorRgba.FromBytes(180, 196, 212));
        }
    }

    private void DrawSearchOverlay(RaidViewModel vm)
    {
        if (vm.ActiveSearchContainer is null) return;

        const float width = 420f;
        const float height = 72f;
        var x = (Context.Window.Size.X - width) * 0.5f;
        var y = Context.Window.Size.Y * 0.36f;

        var panel = new RectF(x, y, width, height);
        Fill(panel, ColorRgba.FromBytes(14, 17, 22, 232));
        DrawOutline(panel, 2f, ColorRgba.FromBytes(168, 180, 192));

        DrawText(new Vector2(x + 18f, y + 12f),
            $"Обыск: {vm.ActiveSearchContainer.Loot.Label}", 0.40f,
            ColorRgba.FromBytes(232, 218, 168));

        var barRect = new RectF(x + 18f, y + 44f, width - 36f, 14f);
        Fill(barRect, ColorRgba.FromBytes(40, 48, 56, 220));
        DrawOutline(barRect, 1f, ColorRgba.FromBytes(96, 110, 124));

        var fillWidth = (barRect.Width - 2f) * Math.Clamp(vm.SearchProgress, 0f, 1f);
        if (fillWidth > 0.5f)
            Fill(new RectF(barRect.X + 1f, barRect.Y + 1f, fillWidth, barRect.Height - 2f),
                ColorRgba.FromBytes(232, 192, 96));
    }

    public override void Draw()
    {
    }

    private void DrawLeftPanel(string stateName, RaidModel raid, RaidViewModel vm, FrameStats frameStats)
    {
        var panel = new RectF(16f, 16f, 690f, 332f);
        Fill(panel, ColorRgba.FromBytes(18, 22, 28, 216));
        DrawOutline(panel, 2f, ColorRgba.FromBytes(88, 104, 120));

        DrawText(new Vector2(30f, 28f), "HUD / Raid Readout", 0.44f, ColorRgba.FromBytes(236, 239, 244));
        DrawText(new Vector2(30f, 52f), $"State: {stateName}", 0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 70f),
            $"Position: {raid.Player.Transform.Position.X:0.00}  {raid.Player.Transform.Position.Y:0.00}  {raid.Player.Transform.Position.Z:0.00}",
            0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 88f),
            $"Rotation Y: {raid.Player.YawDegrees:0.00}°   Move scale: {raid.PlayerMoveScale:0.00}   Quiet walk: {(raid.QuietMovementActive ? "on" : "off")}",
            0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 106f),
            $"Seed: {raid.Seed}   Modules: {raid.ModuleCount}   Props: {raid.PropCount}   Room x{raid.RoomSizeMultiplier:0.00}",
            0.34f, ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 124f),
            $"Doors: {raid.DoorCount}   Blocked: {raid.LockedPassageCount}   Power: {(raid.PowerRestored ? "restored" : "offline")}",
            0.34f, raid.PowerRestored ? ColorRgba.FromBytes(168, 204, 176) : ColorRgba.FromBytes(231, 179, 96));
        DrawText(new Vector2(30f, 142f),
            $"Lights: {raid.LightCount}   Infected: {raid.ActiveInfectedZoneCount}   Alert: {raid.AlertedHostileCount}   Hunt: {raid.HuntingHostileCount}   Alive: {raid.AliveHostileCount}/{raid.Hostiles.Count}",
            0.34f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(30f, 160f),
            $"Cargo: {raid.CargoCount} items   Value: {raid.CargoValue}   Medkits: {vm.MedkitCount}",
            0.34f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(30f, 178f), $"Objective: {raid.ObjectiveLabel}", 0.34f,
            ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(30f, 196f), $"Pressure: {raid.PressureLabel}   Time: {raid.TimeRemainingSeconds:0.0}s",
            0.34f, GetPressureColor(vm.Phase, raid.PressureLevel));
        DrawText(new Vector2(30f, 214f),
            $"Mutation: {(raid.CriticalMutationActive ? "active" : "dormant")}   Ignore collision: {(raid.IgnoreCollision ? "on" : "off")}",
            0.34f,
            raid.CriticalMutationActive ? ColorRgba.FromBytes(212, 126, 151) : ColorRgba.FromBytes(187, 196, 205));
        DrawText(new Vector2(30f, 232f),
            $"Noise: {raid.PlayerNoiseLevel:0.00}   Visibility: {raid.PlayerVisibilityLevel:0.00}   HP: {vm.PlayerHealth:0}/{vm.PlayerMaxHealth:0}",
            0.34f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(30f, 250f),
            $"FPS: {frameStats.FramesPerSecond:0.0}   Frame: {frameStats.FrameTimeMs:0.00} ms", 0.34f,
            ColorRgba.FromBytes(187, 196, 205));

        var healthRatio = vm.PlayerMaxHealth <= 0.01f ? 0f : vm.PlayerHealth / vm.PlayerMaxHealth;
        DrawMeter(new Vector2(30f, 286f), 190f, 12f, healthRatio,
            ColorRgba.FromBytes(46, 56, 64),
            healthRatio > 0.45f
                ? ColorRgba.FromBytes(112, 191, 128)
                : ColorRgba.FromBytes(212, 92, 92),
            "Health");
        DrawMeter(new Vector2(250f, 286f), 160f, 12f, raid.PlayerNoiseLevel,
            ColorRgba.FromBytes(46, 56, 64),
            ColorRgba.FromBytes(218, 164, 86),
            "Noise");
        DrawMeter(new Vector2(438f, 286f), 160f, 12f, raid.PlayerVisibilityLevel,
            ColorRgba.FromBytes(46, 56, 64),
            ColorRgba.FromBytes(114, 178, 224),
            "Light");
    }

    private void DrawBottomBar()
    {
        var hint = new RectF(16f, Context.Window.Size.Y - 52f, 1000f, 36f);
        Fill(hint, ColorRgba.FromBytes(18, 22, 28, 196));
        DrawOutline(hint, 2f, ColorRgba.FromBytes(88, 104, 120));
        DrawText(new Vector2(28f, Context.Window.Size.Y - 41f),
            "WASD — движение   Shift — тихий шаг   ПКМ — поворот   ЛКМ — заряд удара   E — взаимодействие   Q — аптечка   R — новый сектор   F1 — debug   Escape — в меню",
            0.34f, ColorRgba.FromBytes(214, 220, 227));
    }

    private void DrawContextPanels(RaidViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.ContextHint)) return;

        var contextBox = new RectF(16f, Context.Window.Size.Y - 140f, 680f, 34f);
        Fill(contextBox, ColorRgba.FromBytes(48, 36, 26, 214));
        DrawOutline(contextBox, 2f, ColorRgba.FromBytes(170, 124, 74));
        DrawText(new Vector2(28f, Context.Window.Size.Y - 129f), vm.ContextHint, 0.34f,
            ColorRgba.FromBytes(241, 226, 204));
    }

    private void DrawAttackMeter(RaidModel raid, RaidViewModel vm)
    {
        var combat = raid.Combat;
        const float width = 320f;
        const float height = 18f;
        var x = (Context.Window.Size.X - width) * 0.5f;
        var y = Context.Window.Size.Y - 188f;

        var rect = new RectF(x, y, width, height);
        Fill(rect, ColorRgba.FromBytes(20, 24, 30, 220));
        DrawOutline(rect, 1f, ColorRgba.FromBytes(96, 110, 124));

        var windowStart = Math.Clamp(combat.WindowStart, 0f, 1f);
        var windowEnd = Math.Clamp(combat.WindowEnd, 0f, 1f);
        var windowLeft = x + 1f + (width - 2f) * windowStart;
        var windowWidth = MathF.Max(2f, (width - 2f) * (windowEnd - windowStart));
        Fill(new RectF(windowLeft, y + 1f, windowWidth, height - 2f),
            ColorRgba.FromBytes(232, 184, 64, 200));

        var charge = Math.Clamp(combat.ChargeLevel, 0f, 1f);
        if (charge > 0.001f)
        {
            var fillWidth = (width - 2f) * charge;
            var inSweetSpot = charge >= windowStart && charge <= windowEnd;
            Fill(new RectF(x + 1f, y + 1f, fillWidth, height - 2f),
                inSweetSpot
                    ? ColorRgba.FromBytes(255, 220, 88)
                    : ColorRgba.FromBytes(126, 192, 218));
        }

        if (combat.OnCooldown)
        {
            var fillWidth = (width - 2f) * (1f - combat.CooldownNormalized);
            Fill(new RectF(x + 1f, y + 1f, fillWidth, height - 2f),
                ColorRgba.FromBytes(86, 90, 96, 200));
        }

        var statusLabel = combat.OnCooldown
            ? $"Кулдаун {combat.CooldownNormalized * raid.AttackCooldownSeconds:0.00}s"
            : combat.Charging
                ? $"Заряд {charge:0.00}"
                : "ЛКМ — зарядить удар";
        DrawText(new Vector2(x, y - 18f), statusLabel, 0.32f, ColorRgba.FromBytes(214, 220, 227));

        if (vm.LastAttack is { } last)
        {
            var resultText = last.Critical
                ? $"Критический! Урон {last.Damage:0}"
                : $"Удар. Урон {last.Damage:0}";
            DrawText(new Vector2(x + width + 12f, y - 2f), resultText, 0.32f,
                last.Critical ? ColorRgba.FromBytes(255, 220, 110) : ColorRgba.FromBytes(196, 224, 244));
        }
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

    private static ColorRgba GetPressureColor(RaidPhase phase, RaidPressureLevel pressure)
    {
        return phase switch
        {
            RaidPhase.Extracted => ColorRgba.FromBytes(225, 214, 120),
            RaidPhase.Failed => ColorRgba.FromBytes(232, 122, 122),
            _ => pressure switch
            {
                RaidPressureLevel.Stable => ColorRgba.FromBytes(178, 202, 184),
                RaidPressureLevel.Pressure => ColorRgba.FromBytes(228, 191, 109),
                RaidPressureLevel.Critical => ColorRgba.FromBytes(230, 122, 102),
                _ => ColorRgba.FromBytes(187, 196, 205)
            }
        };
    }
}

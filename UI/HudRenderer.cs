using System.Numerics;
using EFP.Entities;
using EFP.Utilities;
using ImGuiNET;

namespace EFP.UI;

public sealed class HudRenderer
{
    public static void Draw(string stateName, PlayerCube player, FrameStats frameStats)
    {
        ImGui.SetNextWindowPos(new Vector2(12f, 12f), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.82f);

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav;

        ImGui.Begin("HUD", flags);
        ImGui.Text($"State: {stateName}");
        ImGui.Separator();
        ImGui.Text($"Position: {player.Transform.Position.X:0.00}, {player.Transform.Position.Y:0.00}, {player.Transform.Position.Z:0.00}");
        ImGui.Text($"Rotation Y: {player.YawDegrees:0.00}°");
        ImGui.Text($"FPS: {frameStats.FramesPerSecond:0.0}");
        ImGui.Text($"Frame: {frameStats.FrameTimeMs:0.00} ms");
        ImGui.End();
    }
}

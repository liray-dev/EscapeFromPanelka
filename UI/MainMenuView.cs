using System.Numerics;
using ImGuiNET;

namespace EFP.UI;

public sealed class MainMenuView
{
    public static void Draw(Action onStart, Action onExit)
    {
        var displaySize = ImGui.GetIO().DisplaySize;
        var windowSize = new Vector2(500f, 300f);
        var position = (displaySize - windowSize) * 0.5f;

        ImGui.SetNextWindowPos(position, ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoMove;

        ImGui.Begin("Escape From Panelka", flags);
        ImGui.Spacing();

        if (ImGui.Button("Запуск", new Vector2(-1f, 40f)))
        {
            onStart();
        }

        ImGui.Spacing();
        if (ImGui.Button("Выход", new Vector2(-1f, 32f)))
        {
            onExit();
        }

        ImGui.End();
    }
}

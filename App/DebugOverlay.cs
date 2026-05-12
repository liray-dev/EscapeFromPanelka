using System.Numerics;
using EFP.Utilities;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace EFP.App;

public sealed class DebugOverlay(GL gl, IWindow window, IInputContext inputContext, DebugSettings settings,
        Action? onSettingsChanged = null)
    : IDisposable
{
    private readonly ImGuiController _controller = new(gl, window, inputContext);
    private FrameStats? _frameStats;
    private bool _hasFrame;
    private string _stateName = "None";

    public void Dispose()
    {
        _controller.Dispose();
    }

    public void Update(float deltaTime, string stateName, FrameStats frameStats)
    {
        _stateName = stateName;
        _frameStats = frameStats;

        if (!settings.Enabled)
        {
            settings.CaptureKeyboard = false;
            settings.CaptureMouse = false;
            _hasFrame = false;
            return;
        }

        _controller.Update(deltaTime);
        DrawWindows();

        var io = ImGui.GetIO();
        settings.CaptureKeyboard = io.WantCaptureKeyboard;
        settings.CaptureMouse = io.WantCaptureMouse;
        _hasFrame = true;
    }

    public void Render()
    {
        if (!_hasFrame || !settings.Enabled) return;

        _controller.Render();
        _hasFrame = false;
    }

    private void DrawWindows()
    {
        ImGui.SetNextWindowSize(new Vector2(460f, 640f), ImGuiCond.FirstUseEver);
        var open = settings.Enabled;
        var changed = false;

        if (ImGui.Begin("Panelka Debug", ref open, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.TextUnformatted($"State: {_stateName}");

            if (_frameStats is not null)
            {
                ImGui.TextUnformatted($"FPS: {_frameStats.FramesPerSecond:0.0}");
                ImGui.TextUnformatted($"Frame: {_frameStats.FrameTimeMs:0.00} ms");
            }

            ImGui.Separator();
            ImGui.TextUnformatted("World");

            var ignoreCollisions = settings.IgnoreCollisions;
            if (ImGui.Checkbox("Ignore collisions", ref ignoreCollisions))
            {
                settings.IgnoreCollisions = ignoreCollisions;
                changed = true;
            }

            var allowCriticalMutation = settings.AllowCriticalMutation;
            if (ImGui.Checkbox("Allow critical mutation", ref allowCriticalMutation))
            {
                settings.AllowCriticalMutation = allowCriticalMutation;
                changed = true;
            }

            var roomSizeMultiplier = settings.RoomSizeMultiplier;
            if (ImGui.SliderFloat("Room size multiplier", ref roomSizeMultiplier, 0.75f, 1.80f))
            {
                settings.RoomSizeMultiplier = roomSizeMultiplier;
                changed = true;
            }

            ImGui.Separator();
            ImGui.TextUnformatted("Camera");

            var cameraYaw = settings.CameraYawDegrees;
            if (ImGui.SliderFloat("Camera yaw", ref cameraYaw, -180f, 180f))
            {
                settings.CameraYawDegrees = cameraYaw;
                changed = true;
            }

            var cameraPitch = settings.CameraPitchDegrees;
            if (ImGui.SliderFloat("Camera pitch", ref cameraPitch, 18f, 82f))
            {
                settings.CameraPitchDegrees = cameraPitch;
                changed = true;
            }

            var cameraDistance = settings.CameraDistance;
            if (ImGui.SliderFloat("Camera distance", ref cameraDistance, 4f, 24f))
            {
                settings.CameraDistance = cameraDistance;
                changed = true;
            }

            var cameraHeight = settings.CameraHeight;
            if (ImGui.SliderFloat("Camera height", ref cameraHeight, 2f, 24f))
            {
                settings.CameraHeight = cameraHeight;
                changed = true;
            }

            var followSmoothing = settings.CameraFollowSmoothing;
            if (ImGui.SliderFloat("Follow smoothing", ref followSmoothing, 0.05f, 1.00f))
            {
                settings.CameraFollowSmoothing = followSmoothing;
                changed = true;
            }

            ImGui.Separator();
            ImGui.TextUnformatted("Player");

            var rotationSpeedMultiplier = settings.RotationSpeedMultiplier;
            if (ImGui.SliderFloat("Yaw speed multiplier", ref rotationSpeedMultiplier, 0.10f, 1.40f))
            {
                settings.RotationSpeedMultiplier = rotationSpeedMultiplier;
                changed = true;
            }

            ImGui.Separator();
            ImGui.TextUnformatted("Lighting");

            var lightColor = settings.LightColor;
            if (ImGui.ColorEdit3("Light color", ref lightColor))
            {
                settings.LightColor = Vector3.Clamp(lightColor, Vector3.Zero, Vector3.One);
                changed = true;
            }

            var ambient = settings.AmbientStrength;
            if (ImGui.SliderFloat("Ambient", ref ambient, 0.02f, 1.00f))
            {
                settings.AmbientStrength = ambient;
                changed = true;
            }

            var diffuse = settings.DiffuseStrength;
            if (ImGui.SliderFloat("Diffuse", ref diffuse, 0.02f, 1.20f))
            {
                settings.DiffuseStrength = diffuse;
                changed = true;
            }

            ImGui.Separator();
            ImGui.TextUnformatted("Fog");

            var fogColor = settings.FogColor;
            if (ImGui.ColorEdit3("Fog color", ref fogColor))
            {
                settings.FogColor = Vector3.Clamp(fogColor, Vector3.Zero, Vector3.One);
                changed = true;
            }

            var fogNear = settings.FogNear;
            if (ImGui.SliderFloat("Fog near", ref fogNear, 1.0f, 80.0f))
            {
                settings.FogNear = fogNear;
                if (settings.FogFar < fogNear + 1.0f) settings.FogFar = fogNear + 1.0f;
                changed = true;
            }

            var fogFar = settings.FogFar;
            if (ImGui.SliderFloat("Fog far", ref fogFar, 2.0f, 140.0f))
            {
                settings.FogFar = MathF.Max(fogFar, settings.FogNear + 1.0f);
                changed = true;
            }

            ImGui.Separator();
            ImGui.TextUnformatted("ImGui");

            var showDemoWindow = settings.ShowDemoWindow;
            if (ImGui.Checkbox("Show ImGui demo", ref showDemoWindow))
            {
                settings.ShowDemoWindow = showDemoWindow;
                changed = true;
            }

            ImGui.Separator();
            ImGui.TextWrapped(
                "F1 toggles this overlay. While the window captures input, gameplay controls are paused.");
        }

        ImGui.End();
        if (settings.Enabled != open)
        {
            settings.Enabled = open;
            changed = true;
        }

        if (settings.ShowDemoWindow)
        {
            var showDemoWindow = settings.ShowDemoWindow;
            ImGui.ShowDemoWindow(ref showDemoWindow);
            if (settings.ShowDemoWindow != showDemoWindow)
            {
                settings.ShowDemoWindow = showDemoWindow;
                changed = true;
            }
        }

        if (changed) onSettingsChanged?.Invoke();
    }
}

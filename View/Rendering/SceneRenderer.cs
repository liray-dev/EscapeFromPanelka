using System.Numerics;
using EFP.App;
using EFP.Model.Common;
using EFP.Model.Raid;
using EFP.View.Camera;
using EFP.View.Rendering.Models;
using EFP.View.Resources;
using Silk.NET.OpenGL;
using RaidModel = EFP.Model.Raid.Raid;

namespace EFP.View.Rendering;

public sealed class SceneRenderer(GL gl, GameResources resources, ModelRegistry modelRegistry) : IDisposable
{
    private const int MaxPointLights = 16;
    private Mesh? _cubeMesh;
    private Mesh? _gridMesh;
    private Mesh? _planeMesh;
    private ShaderProgram? _shader;

    public void Dispose()
    {
        _gridMesh?.Dispose();
        _cubeMesh?.Dispose();
        _planeMesh?.Dispose();
        _shader?.Dispose();
    }

    public void Load()
    {
        gl.Enable(GLEnum.DepthTest);
        gl.Disable(GLEnum.CullFace);

        _shader = resources.CreateShaderProgram("shaders/scene/scene.vert", "shaders/scene/scene.frag");

        var plane = MeshFactory.CreatePlane();
        _planeMesh = new Mesh(gl, plane.Vertices, plane.Indices, plane.PrimitiveType);

        var cube = MeshFactory.CreateCube();
        _cubeMesh = new Mesh(gl, cube.Vertices, cube.Indices, cube.PrimitiveType);

        var grid = MeshFactory.CreateGrid(32, 1f, 0.021f);
        _gridMesh = new Mesh(gl, grid.Vertices, grid.Indices, grid.PrimitiveType);
    }

    public void Resize(int width, int height)
    {
        gl.Viewport(0, 0, (uint)Math.Max(1, width), (uint)Math.Max(1, height));
    }

    public void BeginFrame()
    {
        gl.Enable(GLEnum.DepthTest);
        gl.Disable(GLEnum.CullFace);
        gl.ClearColor(0.05f, 0.06f, 0.08f, 1.0f);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    public void RenderWorld(RaidModel world, TopDownCamera camera, DebugSettings debugSettings)
    {
        if (_shader is null || _planeMesh is null || _cubeMesh is null || _gridMesh is null) return;

        var environment = BuildEnvironment(world, debugSettings);
        gl.ClearColor(environment.ClearColor.X, environment.ClearColor.Y, environment.ClearColor.Z,
            environment.ClearColor.W);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        _shader.Use();
        _shader.SetVector3("uLightDirection", environment.LightDirection);
        _shader.SetVector3("uLightColor", environment.LightColor);
        _shader.SetFloat("uAmbientStrength", environment.AmbientStrength);
        _shader.SetFloat("uDiffuseStrength", environment.DiffuseStrength);
        _shader.SetVector3("uFogColor", environment.FogColor);
        _shader.SetFloat("uFogNear", environment.FogNear);
        _shader.SetFloat("uFogFar", environment.FogFar);
        _shader.SetVector3("uCameraPosition", camera.Position);
        ApplyPointLights(world, environment);

        RenderRenderable(world.Foundation, camera);
        RenderMesh(_gridMesh, Matrix4x4.Identity, camera, Vector4.One);

        foreach (var renderable in world.StaticGeometry) RenderRenderable(renderable, camera);
        foreach (var renderable in world.FeatureGeometry) RenderRenderable(renderable, camera);
        foreach (var zone in world.ActiveInfectedZones) RenderRenderable(zone.Renderable, camera);
        foreach (var mutation in world.ActiveCriticalMutationGeometry) RenderRenderable(mutation, camera);
        foreach (var passage in world.ActiveLockablePassages) RenderRenderable(passage.Renderable, camera);
        foreach (var prop in world.Props) RenderRenderable(prop.Renderable, camera);
        foreach (var pickup in world.ActiveLootPickups) RenderRenderable(pickup.Renderable, camera);

        foreach (var hostile in world.Hostiles)
        {
            if (hostile.IsDead) continue;
            RenderEntity(hostile.ModelId, hostile.Transform, hostile.Tint, camera);
        }

        if (world.PowerSwitchMarker is { } powerSwitchMarker) RenderRenderable(powerSwitchMarker, camera);
        if (world.ObjectiveMarker is { } objectiveMarker) RenderRenderable(objectiveMarker, camera);
        if (world.ExtractionMarker is { } extractionMarker) RenderRenderable(extractionMarker, camera);
        foreach (var extract in world.ExtractionPoints)
        {
            if (extract.Used) continue;
            RenderRenderable(extract.Marker, camera);
        }

        var playerTint = world.Phase switch
        {
            RaidPhase.Failed => new Vector4(0.62f, 0.32f, 0.36f, 1f),
            RaidPhase.Extracted => new Vector4(0.82f, 0.84f, 0.58f, 1f),
            _ => Vector4.Lerp(new Vector4(0.52f, 0.62f, 0.76f, 1f), new Vector4(0.92f, 0.92f, 0.92f, 1f),
                world.PlayerVisibilityLevel)
        };

        RenderEntity(world.Player.ModelId, world.Player.Transform, playerTint, camera);
    }

    private void RenderEntity(string? modelId, Transform transform, Vector4 tint, TopDownCamera camera)
    {
        var model = modelRegistry.Resolve(modelId);
        if (model is null)
        {
            RenderMesh(_cubeMesh!, transform.CreateModelMatrix(), camera, tint);
            return;
        }

        var matrix =
            Matrix4x4.CreateScale(transform.Scale * model.ScaleVector)
            * Matrix4x4.CreateFromYawPitchRoll(transform.Rotation.Y + model.YawOffsetRadians,
                transform.Rotation.X, transform.Rotation.Z)
            * Matrix4x4.CreateTranslation(transform.Position + model.Offset);
        RenderMesh(model.Mesh, matrix, camera, tint * model.Tint);
    }

    private void RenderRenderable(WorldRenderable renderable, TopDownCamera camera)
    {
        if (!string.IsNullOrEmpty(renderable.ModelId))
        {
            var model = modelRegistry.Resolve(renderable.ModelId);
            if (model is not null)
            {
                var matrix =
                    Matrix4x4.CreateScale(renderable.Transform.Scale * model.ScaleVector)
                    * Matrix4x4.CreateFromYawPitchRoll(
                        renderable.Transform.Rotation.Y + model.YawOffsetRadians,
                        renderable.Transform.Rotation.X, renderable.Transform.Rotation.Z)
                    * Matrix4x4.CreateTranslation(renderable.Transform.Position + model.Offset);
                RenderMesh(model.Mesh, matrix, camera, renderable.Tint * model.Tint);
                return;
            }
        }

        var mesh = renderable.PrimitiveType switch
        {
            WorldPrimitiveType.Cube => _cubeMesh,
            WorldPrimitiveType.Plane => _planeMesh,
            _ => _cubeMesh
        };

        if (mesh is null) return;

        RenderMesh(mesh, renderable.Transform.CreateModelMatrix(), camera, renderable.Tint);
    }

    private void RenderMesh(Mesh mesh, Matrix4x4 model, TopDownCamera camera, Vector4 tint)
    {
        var mvp = model * camera.View * camera.Projection;
        _shader!.SetMatrix4("uModel", model);
        _shader.SetMatrix4("uMvp", mvp);
        _shader.SetVector4("uTint", tint);
        mesh.Draw();
    }

    public void DrawTracer(Vector3 from, Vector3 to, float width, Vector4 tint, TopDownCamera camera)
    {
        if (_shader is null || _cubeMesh is null) return;

        var diff = to - from;
        var length = diff.Length();
        if (length < 0.01f) return;

        var midpoint = (from + to) * 0.5f;
        var yaw = MathF.Atan2(diff.X, diff.Z);
        var matrix = Matrix4x4.CreateScale(width, width, length)
                     * Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, yaw)
                     * Matrix4x4.CreateTranslation(midpoint);

        gl.DepthMask(false);
        _shader.Use();
        RenderMesh(_cubeMesh, matrix, camera, tint);
        gl.DepthMask(true);
    }

    private void ApplyPointLights(RaidModel world, SceneEnvironment environment)
    {
        if (_shader is null) return;

        var lightCount = Math.Min(MaxPointLights, world.Lights.Count);
        _shader.SetInt("uPointLightCount", lightCount);

        for (var index = 0; index < lightCount; index++)
        {
            var light = world.Lights[index];
            var flicker = light.FlickerSpeed <= 0.01f
                ? 1f
                : 0.82f + 0.18f *
                (0.5f + 0.5f * MathF.Sin(world.ElapsedRaidSeconds * light.FlickerSpeed + light.PhaseOffset));

            var pressureFactor = world.PressureLevel switch
            {
                RaidPressureLevel.Stable => 1.00f,
                RaidPressureLevel.Pressure => light.Emergency ? 0.90f : 0.96f,
                RaidPressureLevel.Critical => light.Emergency ? 0.72f : 0.84f,
                _ => 1.00f
            };

            var color = light.Color;
            if (light.Emergency && world.PressureLevel == RaidPressureLevel.Pressure)
                color = Vector3.Lerp(color, new Vector3(0.94f, 0.68f, 0.54f), 0.18f);
            if (light.Emergency && world.PressureLevel == RaidPressureLevel.Critical)
                color = Vector3.Lerp(color, new Vector3(1.00f, 0.30f, 0.36f), 0.48f);

            color = Vector3.Clamp(color * Vector3.Lerp(Vector3.One, environment.LightColor, 0.20f), Vector3.Zero,
                new Vector3(2.5f));
            var intensity = light.Intensity * flicker * pressureFactor;

            _shader.SetVector3($"uPointLightPosition[{index}]", light.Position);
            _shader.SetVector3($"uPointLightColor[{index}]", color);
            _shader.SetFloat($"uPointLightRadius[{index}]", light.Radius);
            _shader.SetFloat($"uPointLightIntensity[{index}]", intensity);
        }
    }

    private static SceneEnvironment BuildEnvironment(RaidModel world, DebugSettings debugSettings)
    {
        var lightFactor = world.PressureLevel switch
        {
            RaidPressureLevel.Stable => 1.00f,
            RaidPressureLevel.Pressure => 0.82f,
            RaidPressureLevel.Critical => 0.62f,
            _ => 1.00f
        };

        var ambientFactor = world.PressureLevel switch
        {
            RaidPressureLevel.Stable => 1.00f,
            RaidPressureLevel.Pressure => 0.94f,
            RaidPressureLevel.Critical => 0.78f,
            _ => 1.00f
        };

        var fogNearFactor = world.PressureLevel switch
        {
            RaidPressureLevel.Stable => 1.00f,
            RaidPressureLevel.Pressure => 0.72f,
            RaidPressureLevel.Critical => 0.44f,
            _ => 1.00f
        };

        var fogFarFactor = world.PressureLevel switch
        {
            RaidPressureLevel.Stable => 1.00f,
            RaidPressureLevel.Pressure => 0.72f,
            RaidPressureLevel.Critical => 0.42f,
            _ => 1.00f
        };

        var lightColor = debugSettings.LightColor * lightFactor;
        var fogColor = debugSettings.FogColor;

        if (world.ActiveInfectedZoneCount > 0)
        {
            fogColor = Vector3.Lerp(fogColor, new Vector3(0.18f, 0.08f, 0.14f),
                0.20f + world.ActiveInfectedZoneCount * 0.05f);
            fogNearFactor *= 0.94f;
            fogFarFactor *= 0.92f;
        }

        if (world.PressureLevel == RaidPressureLevel.Pressure)
        {
            lightColor = Vector3.Lerp(lightColor, new Vector3(0.85f, 0.76f, 0.70f), 0.18f);
            fogColor = Vector3.Lerp(fogColor, new Vector3(0.11f, 0.12f, 0.16f), 0.34f);
        }
        else if (world.PressureLevel == RaidPressureLevel.Critical)
        {
            lightColor = Vector3.Lerp(lightColor, new Vector3(0.92f, 0.34f, 0.42f), 0.38f);
            fogColor = Vector3.Lerp(fogColor, new Vector3(0.24f, 0.08f, 0.15f), 0.62f);
        }

        if (world.CriticalMutationActive)
        {
            fogColor = Vector3.Lerp(fogColor, new Vector3(0.30f, 0.10f, 0.19f), 0.28f);
            fogNearFactor *= 0.82f;
            fogFarFactor *= 0.82f;
        }

        var fogNear = MathF.Max(1.0f, debugSettings.FogNear * fogNearFactor);
        var fogFar = MathF.Max(fogNear + 1.0f, debugSettings.FogFar * fogFarFactor);
        var clearColor = new Vector4(Vector3.Clamp(fogColor * 0.72f, Vector3.Zero, Vector3.One), 1.0f);

        return new SceneEnvironment(
            clearColor,
            Vector3.Normalize(new Vector3(-0.45f, 1.00f, -0.30f)),
            Vector3.Clamp(lightColor, Vector3.Zero, new Vector3(2.0f)),
            fogColor,
            debugSettings.AmbientStrength * ambientFactor,
            debugSettings.DiffuseStrength * lightFactor,
            fogNear,
            fogFar);
    }

    private readonly record struct SceneEnvironment(
        Vector4 ClearColor,
        Vector3 LightDirection,
        Vector3 LightColor,
        Vector3 FogColor,
        float AmbientStrength,
        float DiffuseStrength,
        float FogNear,
        float FogFar);
}
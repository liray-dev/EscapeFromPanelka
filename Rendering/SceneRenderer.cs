using System.Numerics;
using EFP.Resources;
using EFP.Scene;
using Silk.NET.OpenGL;
using WorldModel = EFP.World.World;

namespace EFP.Rendering;

public sealed class SceneRenderer(GL gl, GameResources resources) : IDisposable
{
    private Mesh? _cubeMesh;
    private Mesh? _planeMesh;

    private ShaderProgram? _shader;

    public void Dispose()
    {
        _cubeMesh?.Dispose();
        _planeMesh?.Dispose();
        _shader?.Dispose();
    }

    public void Load()
    {
        gl.Enable(GLEnum.DepthTest);

        _shader = resources.CreateShaderProgram("shaders/scene/scene.vert", "shaders/scene/scene.frag");

        var plane = MeshFactory.CreatePlane();
        _planeMesh = new Mesh(gl, plane.Vertices, plane.Indices);

        var cube = MeshFactory.CreateCube();
        _cubeMesh = new Mesh(gl, cube.Vertices, cube.Indices);
    }

    public void Resize(int width, int height)
    {
        gl.Viewport(0, 0, (uint)Math.Max(1, width), (uint)Math.Max(1, height));
    }

    public void BeginFrame(Vector4 clearColor)
    {
        gl.Enable(GLEnum.DepthTest);
        gl.Disable(GLEnum.CullFace);
        gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    public void RenderWorld(WorldModel world, TopDownCamera camera)
    {
        if (_shader is null || _planeMesh is null || _cubeMesh is null) return;

        _shader.Use();
        _shader.SetMatrix4("uView", camera.View);
        _shader.SetMatrix4("uProjection", camera.Projection);

        _shader.SetMatrix4("uModel", world.FloorTransform.CreateModelMatrix());
        _planeMesh.Draw();

        _shader.SetMatrix4("uModel", world.Player.Transform.CreateModelMatrix());
        _cubeMesh.Draw();
    }
}
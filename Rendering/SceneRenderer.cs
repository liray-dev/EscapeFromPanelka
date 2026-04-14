using System.Numerics;
using EFP.Resources;
using EFP.Scene;
using Silk.NET.OpenGL;
using WorldModel = EFP.World.World;

namespace EFP.Rendering;

public sealed class SceneRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly GameResources _resources;

    private ShaderProgram? _shader;
    private Mesh? _planeMesh;
    private Mesh? _cubeMesh;
    private Mesh? _gridMesh;

    public SceneRenderer(GL gl, GameResources resources)
    {
        _gl = gl;
        _resources = resources;
    }

    public void Load()
    {
        _gl.Enable(GLEnum.DepthTest);
        _gl.Disable(GLEnum.CullFace);

        _shader = _resources.CreateShaderProgram("shaders/scene/scene.vert", "shaders/scene/scene.frag");

        var plane = MeshFactory.CreatePlane();
        _planeMesh = new Mesh(_gl, plane.Vertices, plane.Indices, plane.PrimitiveType);

        var cube = MeshFactory.CreateCube();
        _cubeMesh = new Mesh(_gl, cube.Vertices, cube.Indices, cube.PrimitiveType);

        var grid = MeshFactory.CreateGrid(12, 1f, 0.015f);
        _gridMesh = new Mesh(_gl, grid.Vertices, grid.Indices, grid.PrimitiveType);
    }

    public void Resize(int width, int height)
    {
        _gl.Viewport(0, 0, (uint)Math.Max(1, width), (uint)Math.Max(1, height));
    }

    public void BeginFrame(Vector4 clearColor)
    {
        _gl.Enable(GLEnum.DepthTest);
        _gl.Disable(GLEnum.CullFace);
        _gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    public void RenderWorld(WorldModel world, TopDownCamera camera)
    {
        if (_shader is null || _planeMesh is null || _cubeMesh is null || _gridMesh is null)
        {
            return;
        }

        _shader.Use();
        RenderMesh(_planeMesh, world.FloorTransform.CreateModelMatrix(), camera);
        RenderMesh(_gridMesh, Matrix4x4.Identity, camera);

        foreach (var blockTransform in world.DebugBlocks)
        {
            RenderMesh(_cubeMesh, blockTransform.CreateModelMatrix(), camera);
        }

        RenderMesh(_cubeMesh, world.Player.Transform.CreateModelMatrix(), camera);
    }

    private void RenderMesh(Mesh mesh, Matrix4x4 model, TopDownCamera camera)
    {
        var mvp = model * camera.View * camera.Projection;
        _shader!.SetMatrix4("uModel", model);
        _shader.SetMatrix4("uMvp", mvp);
        mesh.Draw();
    }

    public void Dispose()
    {
        _gridMesh?.Dispose();
        _cubeMesh?.Dispose();
        _planeMesh?.Dispose();
        _shader?.Dispose();
    }
}

using System.Numerics;
using EFP.Resources;
using EFP.Scene;
using EFP.World;
using Silk.NET.OpenGL;
using WorldModel = EFP.World.World;

namespace EFP.Rendering;

public sealed class SceneRenderer(GL gl, GameResources resources) : IDisposable
{
    private ShaderProgram? _shader;
    private Mesh? _planeMesh;
    private Mesh? _cubeMesh;
    private Mesh? _gridMesh;

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

    public void BeginFrame(Vector4 clearColor)
    {
        gl.Enable(GLEnum.DepthTest);
        gl.Disable(GLEnum.CullFace);
        gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    public void RenderWorld(WorldModel world, TopDownCamera camera)
    {
        if (_shader is null || _planeMesh is null || _cubeMesh is null || _gridMesh is null)
        {
            return;
        }

        _shader.Use();

        RenderRenderable(world.Foundation, camera);
        RenderMesh(_gridMesh, Matrix4x4.Identity, camera, Vector4.One);

        foreach (var renderable in world.StaticGeometry)
        {
            RenderRenderable(renderable, camera);
        }

        foreach (var prop in world.Props)
        {
            RenderRenderable(prop.Renderable, camera);
        }

        RenderMesh(_cubeMesh, world.Player.Transform.CreateModelMatrix(), camera, new Vector4(0.76f, 0.82f, 0.90f, 1f));
    }

    private void RenderRenderable(WorldRenderable renderable, TopDownCamera camera)
    {
        var mesh = renderable.PrimitiveType switch
        {
            WorldPrimitiveType.Cube => _cubeMesh,
            WorldPrimitiveType.Plane => _planeMesh,
            _ => _cubeMesh
        };

        if (mesh is null)
        {
            return;
        }

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

    public void Dispose()
    {
        _gridMesh?.Dispose();
        _cubeMesh?.Dispose();
        _planeMesh?.Dispose();
        _shader?.Dispose();
    }
}
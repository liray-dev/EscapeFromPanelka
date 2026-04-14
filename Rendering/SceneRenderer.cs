using System.Numerics;
using EFP.Scene;
using Silk.NET.OpenGL;
using WorldModel = EFP.World.World;

namespace EFP.Rendering;

public sealed class SceneRenderer(GL gl) : IDisposable
{
    private ShaderProgram? _shader;
    private Mesh? _planeMesh;
    private Mesh? _cubeMesh;

    public void Load()
    {
        gl.Enable(GLEnum.DepthTest);

        _shader = new ShaderProgram(gl, VertexShaderSource, FragmentShaderSource);

        var planeData = MeshFactory.CreatePlane();
        _planeMesh = new Mesh(gl, planeData.Vertices, planeData.Indices);

        var cubeData = MeshFactory.CreateCube();
        _cubeMesh = new Mesh(gl, cubeData.Vertices, cubeData.Indices);
    }

    public void Resize(int width, int height)
    {
        gl.Viewport(0, 0, (uint)Math.Max(1, width), (uint)Math.Max(1, height));
    }

    public void BeginFrame(Vector4 clearColor)
    {
        gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    public void RenderWorld(WorldModel world, TopDownCamera camera)
    {
        if (_shader is null || _planeMesh is null || _cubeMesh is null)
        {
            return;
        }

        _shader.Use();
        _shader.SetMatrix4("uView", camera.View);
        _shader.SetMatrix4("uProjection", camera.Projection);

        _shader.SetMatrix4("uModel", world.FloorTransform.CreateModelMatrix());
        _planeMesh.Draw();

        _shader.SetMatrix4("uModel", world.Player.Transform.CreateModelMatrix());
        _cubeMesh.Draw();
    }

    public void Dispose()
    {
        _cubeMesh?.Dispose();
        _planeMesh?.Dispose();
        _shader?.Dispose();
    }

    private const string VertexShaderSource = """
                                              #version 330 core
                                              layout (location = 0) in vec3 aPosition;
                                              layout (location = 1) in vec3 aColor;

                                              uniform mat4 uModel;
                                              uniform mat4 uView;
                                              uniform mat4 uProjection;

                                              out vec3 vColor;

                                              void main()
                                              {
                                                  vColor = aColor;
                                                  gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
                                              }
                                              """;

    private const string FragmentShaderSource = """
                                                #version 330 core
                                                in vec3 vColor;
                                                out vec4 FragColor;

                                                void main()
                                                {
                                                    FragColor = vec4(vColor, 1.0);
                                                }
                                                """;
}
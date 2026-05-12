using System.Numerics;
using EFP.View.Camera;
using EFP.View.Resources;
using Silk.NET.OpenGL;

namespace EFP.View.Rendering;

public readonly record struct SwingSample(Vector3 Position, Vector3 Color, float Alpha, float Width);

public sealed class SwingRibbon : IDisposable
{
    private const int MaxSamples = 48;
    private const int FloatsPerVertex = 8;

    private readonly uint _ebo;
    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly float[] _vertices = new float[MaxSamples * 2 * FloatsPerVertex];

    public unsafe SwingRibbon(GL gl, GameResources resources)
    {
        _gl = gl;
        _shader = resources.CreateShaderProgram("shaders/vfx/swing.vert", "shaders/vfx/swing.frag");

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Length * sizeof(float)), null,
            BufferUsageARB.DynamicDraw);

        var indices = new uint[(MaxSamples - 1) * 6];
        for (var i = 0; i < MaxSamples - 1; i++)
        {
            var baseV = (uint)(i * 2);
            var idx = i * 6;
            indices[idx + 0] = baseV;
            indices[idx + 1] = baseV + 2;
            indices[idx + 2] = baseV + 1;
            indices[idx + 3] = baseV + 1;
            indices[idx + 4] = baseV + 2;
            indices[idx + 5] = baseV + 3;
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* p = indices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), p,
                BufferUsageARB.StaticDraw);
        }

        const uint stride = FloatsPerVertex * sizeof(float);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, (void*)(6 * sizeof(float)));
        _gl.EnableVertexAttribArray(3);
        _gl.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, (void*)(7 * sizeof(float)));

        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _shader.Dispose();
    }

    public unsafe void Draw(IReadOnlyList<SwingSample> samples, TopDownCamera camera)
    {
        if (samples.Count < 2) return;
        var sampleCount = Math.Min(samples.Count, MaxSamples);

        for (var i = 0; i < sampleCount; i++)
        {
            var current = samples[i];
            Vector3 tangent;
            if (i == 0)
                tangent = samples[1].Position - current.Position;
            else if (i == sampleCount - 1)
                tangent = current.Position - samples[i - 1].Position;
            else
                tangent = samples[i + 1].Position - samples[i - 1].Position;

            tangent.Y = 0f;
            if (tangent.LengthSquared() < 0.0001f) tangent = Vector3.UnitX;
            tangent = Vector3.Normalize(tangent);

            var perpendicular = Vector3.Cross(Vector3.UnitY, tangent);
            var halfWidth = MathF.Max(0.01f, current.Width) * 0.5f;
            var innerPos = current.Position - perpendicular * halfWidth;
            var outerPos = current.Position + perpendicular * halfWidth;

            var baseIdx = i * 2 * FloatsPerVertex;
            WriteVertex(baseIdx, innerPos, current.Color, current.Alpha, -1f);
            WriteVertex(baseIdx + FloatsPerVertex, outerPos, current.Color, current.Alpha, 1f);
        }

        var vertexFloats = sampleCount * 2 * FloatsPerVertex;
        var indexCount = (sampleCount - 1) * 6;

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* p = _vertices)
        {
            _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(vertexFloats * sizeof(float)), p);
        }

        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
        _gl.DepthMask(false);

        _shader.Use();
        _shader.SetMatrix4("uMvp", camera.View * camera.Projection);

        _gl.DrawElements(PrimitiveType.Triangles, (uint)indexCount, DrawElementsType.UnsignedInt, null);

        _gl.DepthMask(true);
        _gl.Disable(GLEnum.Blend);
        _gl.BindVertexArray(0);
    }

    private void WriteVertex(int baseIdx, Vector3 position, Vector3 color, float alpha, float across)
    {
        _vertices[baseIdx + 0] = position.X;
        _vertices[baseIdx + 1] = position.Y;
        _vertices[baseIdx + 2] = position.Z;
        _vertices[baseIdx + 3] = color.X;
        _vertices[baseIdx + 4] = color.Y;
        _vertices[baseIdx + 5] = color.Z;
        _vertices[baseIdx + 6] = alpha;
        _vertices[baseIdx + 7] = across;
    }
}

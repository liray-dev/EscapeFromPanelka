using Silk.NET.OpenGL;

namespace EFP.View.Rendering;

public sealed class Mesh : IDisposable
{
    private readonly uint _ebo;
    private readonly GL _gl;
    private readonly uint _indexCount;
    private readonly PrimitiveType _primitiveType;
    private readonly uint _vao;
    private readonly uint _vbo;

    public unsafe Mesh(GL gl, float[] vertices, uint[] indices, PrimitiveType primitiveType = PrimitiveType.Triangles)
    {
        _gl = gl;
        _primitiveType = primitiveType;
        _indexCount = (uint)indices.Length;

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* verticesPtr = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), verticesPtr,
                BufferUsageARB.StaticDraw);
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* indicesPtr = indices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), indicesPtr,
                BufferUsageARB.StaticDraw);
        }

        const uint stride = 9 * sizeof(float);

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));

        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*)(6 * sizeof(float)));

        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
    }

    public void Draw()
    {
        unsafe
        {
            _gl.BindVertexArray(_vao);
            _gl.DrawElements(_primitiveType, _indexCount, DrawElementsType.UnsignedInt, null);
            _gl.BindVertexArray(0);
        }
    }
}
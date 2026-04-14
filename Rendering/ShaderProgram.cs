using System.Numerics;
using Silk.NET.OpenGL;

namespace EFP.Rendering;

public sealed class ShaderProgram : IDisposable
{
    private readonly GL _gl;

    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;
        Handle = CreateProgram(vertexSource, fragmentSource);
    }

    public uint Handle { get; }

    public void Use()
    {
        _gl.UseProgram(Handle);
    }

    public unsafe void SetMatrix4(string uniformName, Matrix4x4 value)
    {
        var location = _gl.GetUniformLocation(Handle, uniformName);
        if (location < 0)
        {
            return;
        }

        _gl.UniformMatrix4(location, 1, true, &value.M11);
    }

    public unsafe void SetVector3(string uniformName, Vector3 value)
    {
        var location = _gl.GetUniformLocation(Handle, uniformName);
        if (location >= 0)
        {
            _gl.Uniform3(location, value.X, value.Y, value.Z);
        }
    }

    public unsafe void SetVector4(string uniformName, Vector4 value)
    {
        var location = _gl.GetUniformLocation(Handle, uniformName);
        if (location >= 0)
        {
            _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }
    }

    public void SetInt(string uniformName, int value)
    {
        var location = _gl.GetUniformLocation(Handle, uniformName);
        if (location >= 0)
        {
            _gl.Uniform1(location, value);
        }
    }

    public void Dispose()
    {
        _gl.DeleteProgram(Handle);
    }

    private uint CreateProgram(string vertexSource, string fragmentSource)
    {
        var vertexShader = CompileShader(vertexSource, ShaderType.VertexShader);
        var fragmentShader = CompileShader(fragmentSource, ShaderType.FragmentShader);

        var program = _gl.CreateProgram();
        _gl.AttachShader(program, vertexShader);
        _gl.AttachShader(program, fragmentShader);
        _gl.LinkProgram(program);
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);

        if (status == 0)
        {
            var infoLog = _gl.GetProgramInfoLog(program);
            throw new InvalidOperationException($"Shader program link failed: {infoLog}");
        }

        _gl.DetachShader(program, vertexShader);
        _gl.DetachShader(program, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        return program;
    }

    private uint CompileShader(string source, ShaderType shaderType)
    {
        var shader = _gl.CreateShader(shaderType);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);

        if (status == 0)
        {
            var infoLog = _gl.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"{shaderType} compilation failed: {infoLog}");
        }

        return shader;
    }
}

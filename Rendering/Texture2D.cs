using Silk.NET.OpenGL;
using StbImageSharp;

namespace EFP.Rendering;

public sealed class Texture2D : IDisposable
{
    private readonly GL _gl;

    private Texture2D(GL gl, uint handle, int width, int height)
    {
        _gl = gl;
        Handle = handle;
        Width = width;
        Height = height;
    }

    private uint Handle { get; }
    public int Width { get; }
    public int Height { get; }

    public void Dispose()
    {
        _gl.DeleteTexture(Handle);
    }

    public void Bind(TextureUnit textureUnit = TextureUnit.Texture0)
    {
        _gl.ActiveTexture(textureUnit);
        _gl.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public static unsafe Texture2D LoadFromFile(GL gl, string path)
    {
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        var texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, texture);

        fixed (byte* data = image.Data)
        {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)image.Width, (uint)image.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, data);
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

        return new Texture2D(gl, texture, image.Width, image.Height);
    }
}
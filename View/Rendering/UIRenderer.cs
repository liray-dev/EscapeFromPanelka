using System.Numerics;
using EFP.View.Resources;
using EFP.Utilities;
using Silk.NET.OpenGL;

namespace EFP.View.Rendering;

public sealed class UiRenderer(GL gl, GameResources resources) : IDisposable
{
    private int _canvasHeight;
    private int _canvasWidth;
    private uint _ebo;

    private ShaderProgram? _shader;
    private uint _vao;
    private uint _vbo;

    private FontAtlas DefaultFont => resources.GetFont("ui/panelka");

    public void Dispose()
    {
        if (_vao != 0) gl.DeleteVertexArray(_vao);

        if (_vbo != 0) gl.DeleteBuffer(_vbo);

        if (_ebo != 0) gl.DeleteBuffer(_ebo);

        _shader?.Dispose();
    }

    public unsafe void Load()
    {
        _shader = resources.CreateShaderProgram("shaders/ui/ui.vert", "shaders/ui/ui.frag");

        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        _ebo = gl.GenBuffer();

        gl.BindVertexArray(_vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer, 4 * 8 * sizeof(float), null, BufferUsageARB.DynamicDraw);

        var indices = new uint[] { 0, 1, 2, 2, 3, 0 };
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* indicesPtr = indices)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), indicesPtr,
                BufferUsageARB.StaticDraw);
        }

        const uint stride = 8 * sizeof(float);

        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);

        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));

        gl.EnableVertexAttribArray(2);
        gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(4 * sizeof(float)));

        gl.BindVertexArray(0);
    }

    public void BeginFrame(int width, int height)
    {
        _canvasWidth = Math.Max(1, width);
        _canvasHeight = Math.Max(1, height);

        gl.Disable(GLEnum.DepthTest);
        gl.Disable(GLEnum.CullFace);
        gl.Enable(GLEnum.Blend);
        gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        _shader!.Use();
        _shader.SetInt("uTexture0", 0);
    }

    public void EndFrame()
    {
        gl.BindVertexArray(0);
        gl.BindTexture(TextureTarget.Texture2D, 0);
        gl.Disable(GLEnum.Blend);
    }

    public void DrawRect(RectF rect, ColorRgba color)
    {
        DrawTexturedQuad(resources.GetTexture("ui/white"), rect, new RectF(0f, 0f, 1f, 1f), color);
    }

    public void DrawOutline(RectF rect, float thickness, ColorRgba color)
    {
        DrawRect(new RectF(rect.X, rect.Y, rect.Width, thickness), color);
        DrawRect(new RectF(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawRect(new RectF(rect.X, rect.Y, thickness, rect.Height), color);
        DrawRect(new RectF(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public void DrawTexture(Texture2D texture, RectF rect, ColorRgba tint)
    {
        DrawTexturedQuad(texture, rect, new RectF(0f, 0f, 1f, 1f), tint);
    }

    public void DrawText(string text, Vector2 position, float scale, ColorRgba color)
    {
        var font = DefaultFont;
        var pen = position;

        foreach (var character in text)
        {
            if (character == '\n')
            {
                pen.X = position.X;
                pen.Y += font.LineHeight * scale;
                continue;
            }

            var glyph = font.GetGlyph(character);
            if (glyph.Advance <= 0f) continue;

            var glyphRect = new RectF(
                pen.X + glyph.OffsetX * scale,
                pen.Y + glyph.OffsetY * scale,
                glyph.PixelRect.Width * scale,
                glyph.PixelRect.Height * scale);

            DrawTexturedQuad(font.Texture, glyphRect, glyph.UvRect, color);
            pen.X += glyph.Advance * scale;
        }
    }

    public Vector2 MeasureText(string text, float scale)
    {
        return DefaultFont.MeasureText(text, scale);
    }

    private unsafe void DrawTexturedQuad(Texture2D texture, RectF rect, RectF uvRect, ColorRgba color)
    {
        var left = ToClipX(rect.X);
        var right = ToClipX(rect.Right);
        var top = ToClipY(rect.Y);
        var bottom = ToClipY(rect.Bottom);

        var vertices = stackalloc float[]
        {
            left, top, uvRect.X, uvRect.Y, color.R, color.G, color.B, color.A,
            right, top, uvRect.Right, uvRect.Y, color.R, color.G, color.B, color.A,
            right, bottom, uvRect.Right, uvRect.Bottom, color.R, color.G, color.B, color.A,
            left, bottom, uvRect.X, uvRect.Bottom, color.R, color.G, color.B, color.A
        };

        _shader!.Use();
        texture.Bind();

        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, 4 * 8 * sizeof(float), vertices);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
    }

    private float ToClipX(float x)
    {
        return x / _canvasWidth * 2f - 1f;
    }

    private float ToClipY(float y)
    {
        return 1f - y / _canvasHeight * 2f;
    }
}
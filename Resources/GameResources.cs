using System.Text.Json;
using System.Text.Json.Serialization;
using EFP.Rendering;
using EFP.Utilities;
using Silk.NET.OpenGL;

namespace EFP.Resources;

public sealed class GameResources(string rootPath) : IDisposable
{
    private readonly Dictionary<string, FontAtlas> _fonts = [];
    private readonly Dictionary<string, Texture2D> _textures = [];

    private GL? _gl;

    public void Dispose()
    {
        foreach (var texture in _textures.Values) texture.Dispose();

        _textures.Clear();
        _fonts.Clear();
    }

    public void Initialize(GL gl)
    {
        _gl = gl;

        LoadTexture("ui/white", "textures/ui/white.png");
        LoadFont("ui/panelka", "fonts/panelka/panelka_font.json");
    }

    public ShaderProgram CreateShaderProgram(string vertexRelativePath, string fragmentRelativePath)
    {
        EnsureInitialized();
        return new ShaderProgram(_gl!, ReadTextAsset(vertexRelativePath), ReadTextAsset(fragmentRelativePath));
    }

    public Texture2D GetTexture(string key)
    {
        return _textures[key];
    }

    public FontAtlas GetFont(string key)
    {
        return _fonts[key];
    }

    public T LoadConfig<T>(string relativePath) where T : class
    {
        var path = Resolve(relativePath);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        }) ?? throw new InvalidOperationException($"Failed to deserialize config: {relativePath}");
    }

    public string GetModelPath(string relativePath)
    {
        return Resolve(Path.Combine("models", relativePath));
    }

    public string GetSoundPath(string relativePath)
    {
        return Resolve(Path.Combine("sounds", relativePath));
    }

    public string ReadTextAsset(string relativePath)
    {
        return File.ReadAllText(Resolve(relativePath));
    }

    private Texture2D LoadTexture(string key, string relativePath)
    {
        EnsureInitialized();
        var texture = Texture2D.LoadFromFile(_gl!, Resolve(relativePath));
        _textures[key] = texture;
        return texture;
    }

    private FontAtlas LoadFont(string key, string relativePath)
    {
        EnsureInitialized();

        var metaPath = Resolve(relativePath);
        var json = File.ReadAllText(metaPath);
        var data = JsonSerializer.Deserialize<FontAtlasData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException($"Failed to load font atlas metadata: {relativePath}");

        var textureRelativePath = Path.Combine(Path.GetDirectoryName(relativePath) ?? string.Empty, data.Texture)
            .Replace('\\', '/');

        var textureKey = $"font/{key}";
        var texture = LoadTexture(textureKey, textureRelativePath);
        var glyphs = new Dictionary<int, FontGlyph>(data.Glyphs.Count);

        foreach (var glyph in data.Glyphs)
        {
            var uvRect = new RectF(
                glyph.X / (float)texture.Width,
                glyph.Y / (float)texture.Height,
                glyph.Width / (float)texture.Width,
                glyph.Height / (float)texture.Height);

            var pixelRect = new RectF(glyph.X, glyph.Y, glyph.Width, glyph.Height);
            glyphs[glyph.Codepoint] = new FontGlyph(pixelRect, uvRect, glyph.OffsetX, glyph.OffsetY, glyph.Advance);
        }

        var atlas = new FontAtlas(texture, data.LineHeight, data.BaseSize, glyphs);
        _fonts[key] = atlas;
        return atlas;
    }

    private string Resolve(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(rootPath, normalized);
    }

    private void EnsureInitialized()
    {
        if (_gl is null) throw new InvalidOperationException("Resources are not initialized.");
    }
}
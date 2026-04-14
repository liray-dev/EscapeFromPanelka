using System.Text.Json;

namespace EFP.App;

public static class GameConfigLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static GameConfig LoadOrCreate(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        if (!File.Exists(path))
        {
            var config = new GameConfig();
            Save(path, config);
            return config;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GameConfig>(json, SerializerOptions) ?? new GameConfig();
    }

    private static void Save(string path, GameConfig config)
    {
        var json = JsonSerializer.Serialize(config, SerializerOptions);
        File.WriteAllText(path, json);
    }
}
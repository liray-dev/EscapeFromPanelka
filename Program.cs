using EFP.App;

var appDataDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "EscapeFromPanelka");
Directory.CreateDirectory(appDataDir);
var configPath = Path.Combine(appDataDir, "settings.json");
var config = GameConfigLoader.LoadOrCreate(configPath);

using var app = new GameApp(config, configPath);
app.Run();

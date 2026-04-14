using EFP.App;

var configPath = Path.Combine(AppContext.BaseDirectory, "assets", "config", "gameconfig.json");
var config = GameConfigLoader.LoadOrCreate(configPath);

using var app = new GameApp(config);
app.Run();
using EFP.App;

var configPath = Path.Combine(AppContext.BaseDirectory, "config", "gameconfig.json");
var config = GameConfigLoader.LoadOrCreate(configPath);

using var app = new GameApp(config);
app.Run();
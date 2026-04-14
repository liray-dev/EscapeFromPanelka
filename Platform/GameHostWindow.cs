using EFP.App;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace EFP.Platform;

public sealed class GameHostWindow : IDisposable
{
    public GameHostWindow(WindowConfig config)
    {
        var options = WindowOptions.Default;
        options.Title = config.Title;
        options.Size = new Vector2D<int>(config.Width, config.Height);
        options.VSync = config.VSync;

        Window = Silk.NET.Windowing.Window.Create(options);
    }

    public IWindow Window { get; }

    public void Run()
    {
        Window.Run();
    }

    public void Dispose()
    {
        Window.Dispose();
    }
}
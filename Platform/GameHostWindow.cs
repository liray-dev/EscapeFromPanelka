using EFP.App;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace EFP.Platform;

public sealed class GameHostWindow : IDisposable
{
    public GameHostWindow(WindowConfig config)
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(config.Width, config.Height);
        options.Title = config.Title;
        options.VSync = config.VSync;
        options.PreferredDepthBufferBits = 24;
        options.PreferredStencilBufferBits = 8;
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible,
            new APIVersion(3, 3));

        Window = Silk.NET.Windowing.Window.Create(options);
    }

    public IWindow Window { get; }

    public void Dispose()
    {
        Window.Dispose();
    }

    public void Run()
    {
        Window.Run();
    }
}
namespace EFP.Utilities;

public sealed class FrameStats
{
    private double _smoothedDeltaTime = 1.0 / 60.0;

    public double FrameTimeMs { get; private set; }
    public double FramesPerSecond { get; private set; } = 60.0;

    public void Update(double deltaTime)
    {
        if (deltaTime <= 0)
        {
            return;
        }

        _smoothedDeltaTime = (_smoothedDeltaTime * 0.9) + (deltaTime * 0.1);
        FrameTimeMs = _smoothedDeltaTime * 1000.0;
        FramesPerSecond = 1.0 / _smoothedDeltaTime;
    }
}
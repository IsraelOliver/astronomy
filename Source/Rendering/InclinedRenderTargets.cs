using Microsoft.Xna.Framework.Graphics;

namespace Astronomia;

public sealed class InclinedRenderTargets : System.IDisposable
{
    private const int MultiSampleCount = 16;

    private int _width;
    private int _height;

    public RenderTarget2D Scene { get; private set; } = null!;
    public RenderTarget2D BackOrbits { get; private set; } = null!;
    public RenderTarget2D BodyMask { get; private set; } = null!;
    public RenderTarget2D FrontOrbits { get; private set; } = null!;
    public RenderTarget2D Glow { get; private set; } = null!;

    public void EnsureSize(GraphicsDevice graphicsDevice, Viewport viewport)
    {
        if (Scene is not null && _width == viewport.Width && _height == viewport.Height)
            return;

        DisposeTargets();

        _width = viewport.Width;
        _height = viewport.Height;

        Scene = CreateTarget(graphicsDevice);
        BackOrbits = CreateTarget(graphicsDevice);
        BodyMask = CreateTarget(graphicsDevice);
        FrontOrbits = CreateTarget(graphicsDevice);
        Glow = CreateTarget(graphicsDevice);
    }

    public void Dispose()
    {
        DisposeTargets();
    }

    private RenderTarget2D CreateTarget(GraphicsDevice graphicsDevice)
    {
        return new RenderTarget2D(
            graphicsDevice,
            _width,
            _height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            MultiSampleCount,
            RenderTargetUsage.PreserveContents);
    }

    private void DisposeTargets()
    {
        Scene?.Dispose();
        BackOrbits?.Dispose();
        BodyMask?.Dispose();
        FrontOrbits?.Dispose();
        Glow?.Dispose();
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Astronomia;

public sealed class MainMenuState : IGameState
{
    private readonly GraphicsDevice _graphicsDevice;

    public MainMenuState(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void Update(GameTime gameTime)
    {
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(SolarSystemRenderer.BackgroundColor);
    }
}

using Microsoft.Xna.Framework;

namespace Astronomia;

public interface IGameState
{
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}

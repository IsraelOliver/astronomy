using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Astronomia;

public static class TextureFactory
{
    public static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }

    public static Texture2D CreateCircle(GraphicsDevice graphicsDevice, int diameter)
    {
        var texture = new Texture2D(graphicsDevice, diameter, diameter);
        var data = new Color[diameter * diameter];
        var radius = diameter / 2f;
        var center = new Vector2(radius, radius);

        for (var y = 0; y < diameter; y++)
        {
            for (var x = 0; x < diameter; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), center);
                var alpha = MathHelper.Clamp(radius - distance, 0f, 1f);
                data[y * diameter + x] = Color.White * alpha;
            }
        }

        texture.SetData(data);
        return texture;
    }
}

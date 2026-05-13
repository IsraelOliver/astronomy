using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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

    public static Texture2D CreateLine(GraphicsDevice graphicsDevice, int height = 16)
    {
        var texture = new Texture2D(graphicsDevice, 1, height);
        var data = new Color[height];
        var center = (height - 1) * 0.5f;
        var halfSolid = height * 0.28f;
        var feather = MathHelper.Max(height * 0.22f, 1f);

        for (var y = 0; y < height; y++)
        {
            var distance = MathF.Abs(y - center);
            var alpha = 1f - MathHelper.Clamp((distance - halfSolid) / feather, 0f, 1f);
            data[y] = Color.White * alpha;
        }

        texture.SetData(data);
        return texture;
    }
}

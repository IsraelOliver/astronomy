using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Astronomia;

public sealed class PrimitiveRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly TextureAssets _textures;

    public PrimitiveRenderer(SpriteBatch spriteBatch, TextureAssets textures)
    {
        _spriteBatch = spriteBatch;
        _textures = textures;
    }

    public void DrawBody(Vector2 position, float radius, float zoom, Color color)
    {
        var scaledRadius = MathF.Max(1.5f, radius * zoom);
        var destination = new Rectangle(
            (int)(position.X - scaledRadius),
            (int)(position.Y - scaledRadius),
            (int)(scaledRadius * 2f),
            (int)(scaledRadius * 2f));

        _spriteBatch.Draw(_textures.Circle, destination, color);
    }

    public void DrawOrbit(Vector2 center, float radius, float zoom, float orbitTilt, Color color)
    {
        const int segments = 96;
        var previous = OrbitCalculator.GetOrbitPoint(center, radius, 0f, zoom, orbitTilt);

        for (var i = 1; i <= segments; i++)
        {
            var angle = MathHelper.TwoPi * i / segments;
            var current = OrbitCalculator.GetOrbitPoint(center, radius, angle, zoom, orbitTilt);
            DrawLine(previous, current, color, 1f);
            previous = current;
        }
    }

    public void DrawEllipticalOrbit(Vector2 focus, float semiMajorAxis, float eccentricity, float phase, float zoom, Color color)
    {
        const int segments = 160;
        var previous = GetEllipsePoint(focus, semiMajorAxis, eccentricity, phase, 0f, zoom);

        for (var i = 1; i <= segments; i++)
        {
            var trueAnomaly = MathHelper.TwoPi * i / segments;
            var current = GetEllipsePoint(focus, semiMajorAxis, eccentricity, phase, trueAnomaly, zoom);
            DrawLine(previous, current, color, 1f);
            previous = current;
        }
    }

    public void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness)
    {
        const int segments = 56;
        var previous = center + new Vector2(radius, 0f);

        for (var i = 1; i <= segments; i++)
        {
            var angle = MathHelper.TwoPi * i / segments;
            var current = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            DrawLine(previous, current, color, thickness);
            previous = current;
        }
    }

    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        var edge = end - start;
        var length = edge.Length();

        if (length <= 0.001f)
            return;

        var angle = MathF.Atan2(edge.Y, edge.X);
        var source = new Rectangle(0, 0, _textures.Line.Width, _textures.Line.Height);
        var origin = new Vector2(0f, _textures.Line.Height * 0.5f);
        var visualThickness = MathF.Max(1f, thickness);
        var featheredThickness = visualThickness + 2f;
        var scale = new Vector2(length, featheredThickness / _textures.Line.Height);

        _spriteBatch.Draw(_textures.Line, start, source, color, angle, origin, scale, SpriteEffects.None, 0f);
    }

    public void DrawRectangle(Rectangle rectangle, Color color, int thickness)
    {
        _spriteBatch.Draw(_textures.Pixel, new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness), color);
        _spriteBatch.Draw(_textures.Pixel, new Rectangle(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        _spriteBatch.Draw(_textures.Pixel, new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height), color);
        _spriteBatch.Draw(_textures.Pixel, new Rectangle(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), color);
    }

    public void FillRectangle(Rectangle rectangle, Color color)
    {
        _spriteBatch.Draw(_textures.Pixel, rectangle, color);
    }

    private static Vector2 GetEllipsePoint(Vector2 focus, float semiMajorAxis, float eccentricity, float phase, float trueAnomaly, float zoom)
    {
        var radius = semiMajorAxis * (1f - eccentricity * eccentricity) /
            (1f + eccentricity * MathF.Cos(trueAnomaly));
        var angle = trueAnomaly + phase;
        return focus + new Vector2(
            MathF.Cos(angle) * radius * zoom,
            MathF.Sin(angle) * radius * zoom);
    }
}

using Microsoft.Xna.Framework;

namespace Astronomia;

public readonly struct PhysicsVector2
{
    public PhysicsVector2(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }
    public double LengthSquared => X * X + Y * Y;
    public double Length => System.Math.Sqrt(LengthSquared);

    public Vector2 ToRenderVector(float zoom)
    {
        return ToRenderVector(zoom, 1f);
    }

    public Vector2 ToRenderVector(float zoom, float yScale)
    {
        return new Vector2(
            (float)(X * PhysicsConstants.PixelsPerMeter * zoom),
            (float)(Y * PhysicsConstants.PixelsPerMeter * zoom * yScale));
    }

    public static PhysicsVector2 Zero => new(0d, 0d);

    public static PhysicsVector2 operator +(PhysicsVector2 left, PhysicsVector2 right)
    {
        return new PhysicsVector2(left.X + right.X, left.Y + right.Y);
    }

    public static PhysicsVector2 operator -(PhysicsVector2 left, PhysicsVector2 right)
    {
        return new PhysicsVector2(left.X - right.X, left.Y - right.Y);
    }

    public static PhysicsVector2 operator *(PhysicsVector2 vector, double scalar)
    {
        return new PhysicsVector2(vector.X * scalar, vector.Y * scalar);
    }

    public static PhysicsVector2 operator /(PhysicsVector2 vector, double scalar)
    {
        return new PhysicsVector2(vector.X / scalar, vector.Y / scalar);
    }
}

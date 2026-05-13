using Microsoft.Xna.Framework;
using System;

namespace Astronomia;

public static class OrbitCalculator
{
    public const float InclinedOrbitTilt = 0.2f;
    public const float TopDownOrbitTilt = 1f;

    public static float GetOrbitTilt(SystemViewMode viewMode)
    {
        return viewMode == SystemViewMode.TopDown ? TopDownOrbitTilt : InclinedOrbitTilt;
    }

    public static Vector2 GetPlanetPosition(Vector2 center, CelestialBody planet, float simulationDays, float zoom, float orbitTilt)
    {
        return center + GetOrbitOffset(planet, simulationDays, zoom, orbitTilt);
    }

    public static Vector2 GetOrbitOffset(CelestialBody planet, float simulationDays, float zoom, float orbitTilt)
    {
        var angle = MathHelper.TwoPi * (simulationDays / planet.OrbitalPeriodDays) + planet.Phase;
        var scaledRadius = planet.OrbitRadius * zoom;
        return new Vector2(MathF.Cos(angle) * scaledRadius, MathF.Sin(angle) * scaledRadius * orbitTilt);
    }

    public static Vector2 GetOrbitPoint(Vector2 center, float radius, float angle, float zoom, float orbitTilt)
    {
        var scaledRadius = radius * zoom;
        return center + new Vector2(MathF.Cos(angle) * scaledRadius, MathF.Sin(angle) * scaledRadius * orbitTilt);
    }
}

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
        var meanAnomaly = MathHelper.TwoPi * (simulationDays / planet.OrbitalPeriodDays) + planet.Phase;
        return GetOrbitOffsetAtAnomaly(planet, meanAnomaly, zoom, orbitTilt);
    }

    public static Vector2 GetOrbitPoint(Vector2 center, float radius, float angle, float zoom, float orbitTilt)
    {
        var scaledRadius = radius * zoom;
        return center + new Vector2(MathF.Cos(angle) * scaledRadius, MathF.Sin(angle) * scaledRadius * orbitTilt);
    }

    public static Vector2 GetOrbitPoint(Vector2 center, CelestialBody planet, float meanAnomaly, float zoom, float orbitTilt)
    {
        return center + GetOrbitOffsetAtAnomaly(planet, meanAnomaly, zoom, orbitTilt);
    }

    private static Vector2 GetOrbitOffsetAtAnomaly(CelestialBody planet, float meanAnomaly, float zoom, float orbitTilt)
    {
        if (orbitTilt < 0.99f && !HasInclinedOrbit(planet))
        {
            var scaledRadius = planet.OrbitRadius * zoom;
            return new Vector2(MathF.Cos(meanAnomaly) * scaledRadius, MathF.Sin(meanAnomaly) * scaledRadius * orbitTilt);
        }

        var eccentricity = MathHelper.Clamp(planet.Eccentricity, 0f, 0.85f);
        var eccentricAnomaly = SolveEccentricAnomaly(meanAnomaly, eccentricity);
        var semiMajorAxis = planet.OrbitRadius * zoom;
        var semiMinorAxis = semiMajorAxis * MathF.Sqrt(MathF.Max(1f - eccentricity * eccentricity, 0.01f));
        var localX = semiMajorAxis * (MathF.Cos(eccentricAnomaly) - eccentricity);
        var localY = semiMinorAxis * MathF.Sin(eccentricAnomaly);
        var argument = MathHelper.ToRadians(planet.OrbitArgumentDegrees);
        var cosArgument = MathF.Cos(argument);
        var sinArgument = MathF.Sin(argument);
        var rotatedX = localX * cosArgument - localY * sinArgument;
        var rotatedY = localX * sinArgument + localY * cosArgument;
        var planeTilt = MathHelper.ToRadians(planet.OrbitPlaneTiltDegrees);
        var projectedY = rotatedY * orbitTilt * MathF.Cos(planeTilt);

        if (orbitTilt < 0.99f)
            projectedY += rotatedX * MathF.Sin(planeTilt) * 0.3f;

        return new Vector2(rotatedX, projectedY);
    }

    private static float SolveEccentricAnomaly(float meanAnomaly, float eccentricity)
    {
        var anomaly = meanAnomaly;

        for (var i = 0; i < 6; i++)
        {
            var delta = (anomaly - eccentricity * MathF.Sin(anomaly) - meanAnomaly) /
                MathF.Max(1f - eccentricity * MathF.Cos(anomaly), 0.001f);
            anomaly -= delta;
        }

        return anomaly;
    }

    private static bool HasInclinedOrbit(CelestialBody planet)
    {
        return planet.OrbitPlaneTiltDegrees > 0.01f;
    }
}

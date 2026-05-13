using Microsoft.Xna.Framework;
using System;

namespace Astronomia;

public static class BodyPositionService
{
    private const float SatelliteDistanceVisualScale = 72f;
    private const float LunarOrbitInclinationDegrees = 5.14f;
    private const float LunarInclinationVisualBoost = 2.4f;
    private const float LunarAscendingNodeAngle = 0.58f;

    public static Vector2 GetSunPosition(SolarSystemState solarSystem, Vector2 systemCenter, float zoom)
    {
        if (solarSystem.ViewMode != SystemViewMode.TopDown)
            return systemCenter;

        return systemCenter + GetSunOffset(solarSystem, zoom);
    }

    public static Vector2 GetPlanetPosition(SolarSystemState solarSystem, Vector2 systemCenter, CelestialBody planet, float zoom)
    {
        return systemCenter + GetPlanetOffset(solarSystem, planet, zoom);
    }

    public static Vector2 GetSunOffset(SolarSystemState solarSystem, float zoom)
    {
        var sunBody = solarSystem.GravitySimulation.GetBody(solarSystem.Sun.Name);
        return sunBody?.PositionMeters.ToRenderVector(zoom) ?? Vector2.Zero;
    }

    public static Vector2 GetPlanetOffset(SolarSystemState solarSystem, CelestialBody planet, float zoom)
    {
        if (solarSystem.ViewMode == SystemViewMode.TopDown)
        {
            var body = solarSystem.GravitySimulation.GetBody(planet.Name);
            return body?.PositionMeters.ToRenderVector(zoom) ?? Vector2.Zero;
        }

        var orbitTilt = OrbitCalculator.GetOrbitTilt(solarSystem.ViewMode);
        return OrbitCalculator.GetOrbitOffset(planet, solarSystem.SimulationDays, zoom, orbitTilt);
    }

    public static Vector2 GetSatellitePosition(SolarSystemState solarSystem, Vector2 systemCenter, NaturalSatellite satellite, float zoom)
    {
        if (solarSystem.ViewMode != SystemViewMode.TopDown)
        {
            var parent = FindPlanet(solarSystem, satellite.ParentName);
            return parent is null
                ? systemCenter
                : systemCenter +
                    GetPlanetOffset(solarSystem, parent, zoom) +
                    GetSatelliteOrbitVisualOffset(satellite, solarSystem.SimulationDays, zoom, OrbitCalculator.InclinedOrbitTilt);
        }

        var body = solarSystem.GravitySimulation.GetBody(satellite.Name);
        var parentBody = solarSystem.GravitySimulation.GetBody(satellite.ParentName);
        return body is null || parentBody is null
            ? systemCenter
            : systemCenter +
                parentBody.PositionMeters.ToRenderVector(zoom) +
                GetSatelliteVisualOffset(body.PositionMeters - parentBody.PositionMeters, zoom, 1f);
    }

    public static Vector2 GetSatelliteVisualOffset(PhysicsVector2 relativePositionMeters, float zoom, float yScale)
    {
        return relativePositionMeters.ToRenderVector(zoom, yScale) * SatelliteDistanceVisualScale;
    }

    public static float GetSatelliteOrbitVisualRadius(NaturalSatellite satellite, float zoom)
    {
        return (float)(satellite.AverageDistanceMeters * PhysicsConstants.PixelsPerMeter * zoom * SatelliteDistanceVisualScale);
    }

    public static Vector2 GetSatelliteOrbitVisualOffset(NaturalSatellite satellite, float simulationDays, float zoom, float yScale)
    {
        var radius = GetSatelliteOrbitVisualRadius(satellite, zoom);
        var angle = MathHelper.TwoPi * simulationDays / satellite.OrbitalPeriodDays + 0.65f;
        return GetSatelliteOrbitVisualOffsetAtAngle(satellite, radius, angle, yScale);
    }

    public static Vector2 GetSatelliteOrbitVisualOffsetAtAngle(NaturalSatellite satellite, float radius, float angle, float yScale)
    {
        if (satellite.Name != "Lua")
        {
            return new Vector2(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius * yScale);
        }

        var nodeAngle = angle - LunarAscendingNodeAngle;
        var inclination = MathHelper.ToRadians(LunarOrbitInclinationDegrees) * LunarInclinationVisualBoost;
        var x = MathF.Cos(angle) * radius;
        var baseY = MathF.Sin(angle) * radius * yScale;
        var inclinedY = MathF.Sin(nodeAngle) * radius * MathF.Sin(inclination);

        return new Vector2(
            x,
            baseY + inclinedY);
    }

    private static CelestialBody? FindPlanet(SolarSystemState solarSystem, string name)
    {
        foreach (var planet in solarSystem.Planets)
        {
            if (planet.Name == name)
                return planet;
        }

        return null;
    }
}

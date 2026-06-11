using Microsoft.Xna.Framework;
using System;

namespace Astronomia;

public static class SelectionService
{
    public static void SelectBodyAt(
        Vector2 mousePosition,
        SolarSystemState solarSystem,
        Vector2 systemCenter,
        float zoom,
        Rectangle hudPanel,
        Rectangle studyPanel)
    {
        if (Contains(hudPanel, mousePosition) ||
            (solarSystem.HasSelectedBody && Contains(studyPanel, mousePosition)))
            return;

        CelestialBody? closestPlanet = null;
        var sunSelected = false;
        var closestDistance = float.MaxValue;
        var sunHitRadius = MathF.Max(16f, solarSystem.Sun.VisualRadius * zoom + 8f);
        var sunPosition = BodyPositionService.GetSunPosition(solarSystem, systemCenter, zoom);
        var sunDistance = Vector2.Distance(mousePosition, sunPosition);

        if (sunDistance <= sunHitRadius)
        {
            sunSelected = true;
            closestDistance = sunDistance;
        }

        foreach (var planet in solarSystem.Planets)
        {
            var planetPosition = BodyPositionService.GetPlanetPosition(solarSystem, systemCenter, planet, zoom);
            var hitRadius = MathF.Max(14f, planet.Radius * zoom + 9f);

            if (planet.HasRings)
                hitRadius = MathF.Max(hitRadius, planet.Radius * zoom * 2.2f + 5f);

            var distance = Vector2.Distance(mousePosition, planetPosition);

            if (distance <= hitRadius && distance < closestDistance)
            {
                closestPlanet = planet;
                closestDistance = distance;
            }
        }

        if (closestPlanet is not null)
            solarSystem.SelectPlanet(closestPlanet);
        else if (sunSelected && solarSystem.ViewMode != SystemViewMode.TopDown)
            solarSystem.SelectSun();
        else
            solarSystem.ClearSelection();
    }

    private static bool Contains(Rectangle rectangle, Vector2 point)
    {
        return point.X >= rectangle.Left &&
            point.X <= rectangle.Right &&
            point.Y >= rectangle.Top &&
            point.Y <= rectangle.Bottom;
    }
}

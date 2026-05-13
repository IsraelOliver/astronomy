using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Astronomia;

public sealed class SolarSystemRenderer
{
    private static readonly Color SpaceColor = new(5, 8, 18);

    private readonly SpriteBatch _spriteBatch;
    private readonly TextureAssets _textures;
    private readonly ShaderAssets _shaders;
    private readonly PrimitiveRenderer _primitives;
    private readonly InclinedRenderTargets _inclinedTargets = new();
    private float _shaderTime;

    public SolarSystemRenderer(SpriteBatch spriteBatch, TextureAssets textures, ShaderAssets shaders, PrimitiveRenderer primitives)
    {
        _spriteBatch = spriteBatch;
        _textures = textures;
        _shaders = shaders;
        _primitives = primitives;
    }

    public static Color BackgroundColor => SpaceColor;

    public void Draw(SolarSystemState solarSystem, Vector2 center, float zoom, Viewport viewport)
    {
        if (_shaders.PassThrough is null ||
            _shaders.SoftCircleMask is null ||
            _shaders.SunGlow is null ||
            _shaders.ToonPlanet is null ||
            _shaders.SaturnRings is null)
            return;

        var orbitTilt = OrbitCalculator.GetOrbitTilt(solarSystem.ViewMode);
        var sunPosition = BodyPositionService.GetSunPosition(solarSystem, center, zoom);
        _shaderTime = solarSystem.SimulationDays;

        if (solarSystem.ViewMode == SystemViewMode.Inclined)
        {
            DrawInclinedToRenderTarget(solarSystem, center, zoom, viewport, orbitTilt, sunPosition);
            return;
        }

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        DrawStars(solarSystem);
        DrawTrails(solarSystem, center, zoom, orbitTilt);
        DrawSun(solarSystem, sunPosition, zoom);
        DrawPlanets(solarSystem, center, zoom, orbitTilt);
        DrawSatellites(solarSystem, center, zoom);

        if (solarSystem.ShowCenterOfMass)
            DrawCenterOfMass(solarSystem, center, zoom);

        _spriteBatch.End();
    }

    private void DrawInclinedToRenderTarget(
        SolarSystemState solarSystem,
        Vector2 center,
        float zoom,
        Viewport viewport,
        float orbitTilt,
        Vector2 sunPosition)
    {
        var graphicsDevice = _spriteBatch.GraphicsDevice;
        _inclinedTargets.EnsureSize(graphicsDevice, viewport);

        graphicsDevice.SetRenderTarget(_inclinedTargets.BackOrbits);
        graphicsDevice.Clear(Color.Transparent);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        DrawInclinedOrbits(solarSystem, center, zoom, orbitTilt, drawFront: false);
        _spriteBatch.End();

        graphicsDevice.SetRenderTarget(_inclinedTargets.Scene);
        graphicsDevice.Clear(SpaceColor);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        DrawStars(solarSystem);
        _spriteBatch.End();

        DrawSunShaderGlow(solarSystem, sunPosition, zoom, viewport);

        ConfigureSoftCircleMask(viewport, sunPosition, (solarSystem.Sun.VisualRadius + 2f) * zoom, 12f);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, effect: _shaders.SoftCircleMask);
        _spriteBatch.Draw(_inclinedTargets.BackOrbits, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
        _spriteBatch.End();

        DrawMaskedBackRingsForPlanets(solarSystem, center, zoom, viewport, orbitTilt, parentInFront: false);
        DrawMaskedBackSatelliteOrbitsForPlanets(solarSystem, center, zoom, viewport, orbitTilt, parentInFront: false);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        DrawInclinedPlanets(solarSystem, center, zoom, orbitTilt, drawFront: false);
        DrawStylizedSunCore(solarSystem, sunPosition, zoom, viewport);
        DrawInclinedOrbits(solarSystem, center, zoom, orbitTilt, drawFront: true);
        _spriteBatch.End();

        DrawMaskedBackRingsForPlanets(solarSystem, center, zoom, viewport, orbitTilt, parentInFront: true);
        DrawMaskedBackSatelliteOrbitsForPlanets(solarSystem, center, zoom, viewport, orbitTilt, parentInFront: true);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        DrawInclinedPlanets(solarSystem, center, zoom, orbitTilt, drawFront: true);
        _spriteBatch.End();

        graphicsDevice.SetRenderTarget(null);
        graphicsDevice.Clear(SpaceColor);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, effect: _shaders.PassThrough);
        _spriteBatch.Draw(_inclinedTargets.Scene, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
        _spriteBatch.End();
    }

    private void DrawSunShaderGlow(SolarSystemState solarSystem, Vector2 sunPosition, float zoom, Viewport viewport)
    {
        var graphicsDevice = _spriteBatch.GraphicsDevice;

        graphicsDevice.SetRenderTarget(_inclinedTargets.Glow);
        graphicsDevice.Clear(Color.Transparent);

        ConfigureSunGlow(viewport, sunPosition, solarSystem.Sun.VisualRadius * zoom, 1.0f, solarSystem.SimulationDays, coreOnly: false);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, blendState: BlendState.AlphaBlend, effect: _shaders.SunGlow);
        _spriteBatch.Draw(_textures.Pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
        _spriteBatch.End();

        graphicsDevice.SetRenderTarget(_inclinedTargets.Scene);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, blendState: BlendState.Additive);
        _spriteBatch.Draw(_inclinedTargets.Glow, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
        _spriteBatch.End();
    }

    private void DrawStylizedSunCore(SolarSystemState solarSystem, Vector2 sunPosition, float zoom, Viewport viewport)
    {
        ConfigureSunGlow(viewport, sunPosition, solarSystem.Sun.VisualRadius * zoom, 1.0f, solarSystem.SimulationDays, coreOnly: true);

        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, blendState: BlendState.AlphaBlend, effect: _shaders.SunGlow);
        _spriteBatch.Draw(_textures.Pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);

        if (solarSystem.IsSunSelected)
            DrawSelectionMarker(sunPosition, solarSystem.Sun.VisualRadius, zoom);
    }

    private void ConfigureSunGlow(Viewport viewport, Vector2 sunPosition, float sunRadius, float intensity, float time, bool coreOnly)
    {
        _shaders.SunGlow.Parameters["ViewportSize"]?.SetValue(new Vector2(viewport.Width, viewport.Height));
        _shaders.SunGlow.Parameters["SunCenter"]?.SetValue(sunPosition);
        _shaders.SunGlow.Parameters["SunRadius"]?.SetValue(sunRadius);
        _shaders.SunGlow.Parameters["Intensity"]?.SetValue(intensity);
        _shaders.SunGlow.Parameters["Time"]?.SetValue(time);
        _shaders.SunGlow.Parameters["SurfaceNoiseScale"]?.SetValue(2.25f);
        _shaders.SunGlow.Parameters["PulseSpeed"]?.SetValue(0.055f);
        _shaders.SunGlow.Parameters["CoreOnly"]?.SetValue(coreOnly ? 1f : 0f);
    }

    private void ConfigureSoftCircleMask(Viewport viewport, Vector2 maskCenter, float maskRadius, float feather)
    {
        _shaders.SoftCircleMask.Parameters["ViewportSize"]?.SetValue(new Vector2(viewport.Width, viewport.Height));
        _shaders.SoftCircleMask.Parameters["MaskCenter"]?.SetValue(maskCenter);
        _shaders.SoftCircleMask.Parameters["MaskRadius"]?.SetValue(maskRadius);
        _shaders.SoftCircleMask.Parameters["Feather"]?.SetValue(feather);
    }

    private void DrawMaskedBackSatelliteOrbitsForPlanets(
        SolarSystemState solarSystem,
        Vector2 center,
        float zoom,
        Viewport viewport,
        float orbitTilt,
        bool parentInFront)
    {
        var graphicsDevice = _spriteBatch.GraphicsDevice;

        foreach (var planet in solarSystem.Planets)
        {
            var planetPosition = BodyPositionService.GetPlanetPosition(solarSystem, center, planet, zoom);

            if (IsInFrontOfSun(planetPosition, center) != parentInFront)
                continue;

            foreach (var satellite in solarSystem.Satellites)
            {
                if (satellite.ParentName != planet.Name)
                    continue;

                graphicsDevice.SetRenderTarget(_inclinedTargets.BodyMask);
                graphicsDevice.Clear(Color.Transparent);

                _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
                var visualRadius = BodyPositionService.GetSatelliteOrbitVisualRadius(satellite, zoom);
                DrawPerspectiveSatelliteOrbit(
                    satellite,
                    planetPosition,
                    visualRadius / zoom,
                    zoom,
                    orbitTilt,
                    drawFront: false,
                    planetPosition);
                _spriteBatch.End();

                graphicsDevice.SetRenderTarget(_inclinedTargets.Scene);

                var maskRadius = planet.Radius * GetPlanetVisualZoom(planet, planetPosition, center, zoom) + 2f;
                ConfigureSoftCircleMask(viewport, planetPosition, maskRadius, 5f);
                _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, effect: _shaders.SoftCircleMask);
                _spriteBatch.Draw(_inclinedTargets.BodyMask, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
                _spriteBatch.End();
            }
        }
    }

    private void DrawMaskedBackRingsForPlanets(
        SolarSystemState solarSystem,
        Vector2 center,
        float zoom,
        Viewport viewport,
        float orbitTilt,
        bool parentInFront)
    {
        var graphicsDevice = _spriteBatch.GraphicsDevice;

        foreach (var planet in solarSystem.Planets)
        {
            if (!planet.HasRings)
                continue;

            var planetPosition = BodyPositionService.GetPlanetPosition(solarSystem, center, planet, zoom);

            if (IsInFrontOfSun(planetPosition, center) != parentInFront)
                continue;

            graphicsDevice.SetRenderTarget(_inclinedTargets.BodyMask);
            graphicsDevice.Clear(Color.Transparent);

            _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            DrawPerspectiveRings(planetPosition, planet.Radius, zoom, orbitTilt, drawFront: false);
            _spriteBatch.End();

            graphicsDevice.SetRenderTarget(_inclinedTargets.Scene);

            var maskRadius = planet.Radius * GetPlanetVisualZoom(planet, planetPosition, center, zoom) + 2f;
            ConfigureSoftCircleMask(viewport, planetPosition, maskRadius, 4f);
            _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, effect: _shaders.SoftCircleMask);
            _spriteBatch.Draw(_inclinedTargets.BodyMask, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
            _spriteBatch.End();
        }
    }

    private void DrawStars(SolarSystemState solarSystem)
    {
        foreach (var star in solarSystem.Stars)
        {
            var alpha = (byte)(255 * star.Brightness);
            _spriteBatch.Draw(
                _textures.Pixel,
                new Rectangle((int)star.Position.X, (int)star.Position.Y, (int)star.Size, (int)star.Size),
                new Color(alpha, alpha, alpha, alpha));
        }
    }

    private void DrawTrails(SolarSystemState solarSystem, Vector2 center, float zoom, float orbitTilt)
    {
        foreach (var planet in solarSystem.Planets)
        {
            DrawPlanetTrail(solarSystem, planet, center, zoom, orbitTilt);
        }

        foreach (var satellite in solarSystem.Satellites)
        {
            DrawSatelliteTrail(solarSystem, satellite, center, zoom, orbitTilt);
        }
    }

    private void DrawInclinedOrbits(SolarSystemState solarSystem, Vector2 center, float zoom, float orbitTilt, bool drawFront)
    {
        foreach (var planet in solarSystem.Planets)
        {
            DrawPerspectiveOrbit(
                center,
                planet.OrbitRadius,
                zoom,
                orbitTilt,
                0.72f,
                1.35f,
                drawFront,
                center);
        }

    }

    private void DrawPerspectiveOrbit(
        Vector2 center,
        float radius,
        float zoom,
        float orbitTilt,
        float maxAlpha,
        float maxThickness,
        bool drawFront,
        Vector2 depthCenter)
    {
        const int segments = 144;
        var previous = OrbitCalculator.GetOrbitPoint(center, radius, 0f, zoom, orbitTilt);

        for (var i = 1; i <= segments; i++)
        {
            var angle = MathHelper.TwoPi * i / segments;
            var previousAngle = MathHelper.TwoPi * (i - 1) / segments;
            var middleAngle = (previousAngle + angle) * 0.5f;
            var current = OrbitCalculator.GetOrbitPoint(center, radius, angle, zoom, orbitTilt);
            var midpoint = (previous + current) * 0.5f;

            if (IsInFrontOfSun(midpoint, depthCenter) == drawFront)
                DrawPerspectiveOrbitSegment(previous, current, middleAngle, maxAlpha, maxThickness);

            previous = current;
        }
    }

    private void DrawPerspectiveOrbitSegment(Vector2 start, Vector2 end, float angle, float maxAlpha, float maxThickness)
    {
        var frontDepth = (MathF.Sin(angle) + 1f) * 0.5f;
        var glow = MathF.Pow(frontDepth, 0.72f);
        var alpha = MathHelper.Lerp(0.12f, maxAlpha, glow);
        var thickness = MathHelper.Lerp(0.55f, maxThickness, glow);
        var color = Color.Lerp(new Color(44, 61, 108), new Color(156, 195, 255), glow);

        _primitives.DrawLine(start, end, WithAlpha(color, alpha * 0.25f), thickness + 1.8f);
        _primitives.DrawLine(start, end, WithAlpha(color, alpha), thickness);
    }

    private void DrawPlanetTrail(SolarSystemState solarSystem, CelestialBody planet, Vector2 center, float zoom, float orbitTilt)
    {
        DrawFadedTrail(solarSystem.GetPlanetTrail(planet), planet.Color, center, zoom, orbitTilt, 2.6f, 0.86f);
    }

    private void DrawSun(SolarSystemState solarSystem, Vector2 center, float zoom)
    {
        _primitives.DrawBody(center, solarSystem.Sun.VisualRadius + 2f, zoom, SpaceColor);
        _primitives.DrawBody(center, solarSystem.Sun.VisualRadius, zoom, solarSystem.Sun.Color);

        if (solarSystem.IsSunSelected)
            DrawSelectionMarker(center, solarSystem.Sun.VisualRadius, zoom);
    }

    private void DrawPlanets(SolarSystemState solarSystem, Vector2 center, float zoom, float orbitTilt)
    {
        foreach (var planet in solarSystem.Planets)
        {
            var position = BodyPositionService.GetPlanetPosition(solarSystem, center, planet, zoom);
            DrawPlanet(solarSystem, planet, position, zoom, orbitTilt);
        }
    }

    private void DrawInclinedPlanets(SolarSystemState solarSystem, Vector2 center, float zoom, float orbitTilt, bool drawFront)
    {
        var planetsToDraw = new List<(CelestialBody Planet, Vector2 Position)>();

        foreach (var planet in solarSystem.Planets)
        {
            var position = BodyPositionService.GetPlanetPosition(solarSystem, center, planet, zoom);

            if (IsInFrontOfSun(position, center) == drawFront)
                planetsToDraw.Add((planet, position));
        }

        planetsToDraw.Sort((left, right) => left.Position.Y.CompareTo(right.Position.Y));

        foreach (var item in planetsToDraw)
        {
            DrawInclinedPlanetSystem(solarSystem, item.Planet, item.Position, center, zoom, orbitTilt);
        }
    }

    private void DrawInclinedPlanetSystem(
        SolarSystemState solarSystem,
        CelestialBody planet,
        Vector2 planetPosition,
        Vector2 center,
        float zoom,
        float orbitTilt)
    {
        DrawInclinedSatellitesForPlanet(solarSystem, planet, planetPosition, center, zoom, orbitTilt, drawFront: false);
        DrawPlanetBodyWithDepth(planet, planetPosition, center, zoom);

        if (planet.HasRings)
            DrawPerspectiveRings(planetPosition, planet.Radius, zoom, orbitTilt, drawFront: true);

        DrawInclinedSatellitesForPlanet(solarSystem, planet, planetPosition, center, zoom, orbitTilt, drawFront: true);

        if (planet == solarSystem.SelectedPlanet)
            DrawSelectionMarker(planetPosition, planet.Radius, zoom);
    }

    private void DrawPlanet(SolarSystemState solarSystem, CelestialBody planet, Vector2 position, float zoom, float orbitTilt)
    {
        DrawPlanetBody(planet, position, zoom);

        if (planet.HasRings)
            DrawRings(position, planet.Radius, zoom, orbitTilt);

        if (planet == solarSystem.SelectedPlanet)
            DrawSelectionMarker(position, planet.Radius, zoom);
    }

    private void DrawPlanetBody(CelestialBody planet, Vector2 position, float zoom)
    {
        _primitives.DrawBody(position, planet.Radius, zoom, planet.Color);
    }

    private void DrawSatellites(SolarSystemState solarSystem, Vector2 center, float zoom)
    {
        foreach (var satellite in solarSystem.Satellites)
        {
            var position = BodyPositionService.GetSatellitePosition(solarSystem, center, satellite, zoom);
            _primitives.DrawBody(position, satellite.Radius, zoom, satellite.Color);
        }
    }

    private void DrawSatelliteTrail(SolarSystemState solarSystem, NaturalSatellite satellite, Vector2 center, float zoom, float orbitTilt)
    {
        var parent = FindPlanet(solarSystem, satellite.ParentName);

        if (parent is null)
        {
            DrawFadedTrail(solarSystem.GetSatelliteTrail(satellite), satellite.Color, center, zoom, orbitTilt, 1.8f, 0.68f);
            return;
        }

        DrawSatelliteTrailRelativeToParent(solarSystem, satellite, parent, center, zoom, orbitTilt);
    }

    private void DrawFadedTrail(
        IReadOnlyCollection<PhysicsVector2> points,
        Color baseColor,
        Vector2 center,
        float zoom,
        float orbitTilt,
        float maxThickness,
        float maxAlpha)
    {
        if (points.Count < 2)
            return;

        Vector2? previous = null;
        var index = 0;
        var count = points.Count;

        foreach (var point in points)
        {
            var current = center + point.ToRenderVector(zoom, orbitTilt);

            if (previous is not null)
            {
                var progress = index / (float)(count - 1);
                DrawTrailSegment(previous.Value, current, progress, baseColor, maxThickness, maxAlpha);
            }

            previous = current;
            index++;
        }
    }

    private void DrawSatelliteTrailRelativeToParent(
        SolarSystemState solarSystem,
        NaturalSatellite satellite,
        CelestialBody parent,
        Vector2 center,
        float zoom,
        float orbitTilt)
    {
        var satellitePoints = new List<PhysicsVector2>(solarSystem.GetSatelliteTrail(satellite));
        var parentPoints = new List<PhysicsVector2>(solarSystem.GetPlanetTrail(parent));
        var count = Math.Min(satellitePoints.Count, parentPoints.Count);

        if (count < 2)
            return;

        var satelliteStart = satellitePoints.Count - count;
        var parentStart = parentPoints.Count - count;
        Vector2? previous = null;

        for (var i = 0; i < count; i++)
        {
            var parentPosition = parentPoints[parentStart + i];
            var satellitePosition = satellitePoints[satelliteStart + i];
            var relativePosition = satellitePosition - parentPosition;
            var current = center +
                parentPosition.ToRenderVector(zoom, orbitTilt) +
                BodyPositionService.GetSatelliteVisualOffset(relativePosition, zoom, orbitTilt);

            if (previous is not null)
            {
                var progress = i / (float)(count - 1);
                DrawTrailSegment(previous.Value, current, progress, satellite.Color, 1.8f, 0.68f);
            }

            previous = current;
        }
    }

    private void DrawTrailSegment(Vector2 start, Vector2 end, float progress, Color baseColor, float maxThickness, float maxAlpha)
    {
        var intensity = MathF.Pow(progress, 1.85f);
        var alpha = MathHelper.Lerp(0.015f, maxAlpha, intensity);
        var thickness = MathHelper.Lerp(0.55f, maxThickness, intensity);

        _primitives.DrawLine(start, end, WithAlpha(baseColor, alpha * 0.32f), thickness + 2.4f);
        _primitives.DrawLine(start, end, WithAlpha(baseColor, alpha), thickness);
    }

    private void DrawInclinedSatellitesForPlanet(
        SolarSystemState solarSystem,
        CelestialBody planet,
        Vector2 planetPosition,
        Vector2 center,
        float zoom,
        float orbitTilt,
        bool drawFront)
    {
        var satellitesToDraw = new List<(NaturalSatellite Satellite, Vector2 Position)>();

        foreach (var satellite in solarSystem.Satellites)
        {
            if (satellite.ParentName != planet.Name)
                continue;

            if (drawFront)
            {
                var visualRadius = BodyPositionService.GetSatelliteOrbitVisualRadius(satellite, zoom);
                DrawPerspectiveSatelliteOrbit(
                    satellite,
                    planetPosition,
                    visualRadius / zoom,
                    zoom,
                    orbitTilt,
                    drawFront,
                    planetPosition);
            }

            var satellitePosition = BodyPositionService.GetSatellitePosition(solarSystem, center, satellite, zoom);

            if (IsInFrontOfSun(satellitePosition, planetPosition) == drawFront)
                satellitesToDraw.Add((satellite, satellitePosition));
        }

        satellitesToDraw.Sort((left, right) => left.Position.Y.CompareTo(right.Position.Y));

        foreach (var item in satellitesToDraw)
        {
            DrawSatelliteBodyWithDepth(item.Satellite, item.Position, planetPosition, center, zoom);
        }
    }

    private static bool IsInFrontOfSun(Vector2 position, Vector2 sunPosition)
    {
        return position.Y >= sunPosition.Y;
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

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(
            color.R,
            color.G,
            color.B,
            (byte)(MathHelper.Clamp(alpha, 0f, 1f) * 255f));
    }

    private void DrawPlanetBodyWithDepth(CelestialBody planet, Vector2 position, Vector2 depthCenter, float zoom)
    {
        var visualZoom = GetPlanetVisualZoom(planet, position, depthCenter, zoom);
        var depth = GetPlanetDepth(planet, position, depthCenter, zoom);
        var color = Color.Lerp(ScaleColor(planet.Color, 0.58f), ScaleColor(planet.Color, 1.08f), depth);
        DrawToonPlanet(position, planet.Radius, visualZoom, color, depthCenter, 1.0f);
    }

    private static float GetPlanetDepth(CelestialBody planet, Vector2 position, Vector2 depthCenter, float zoom)
    {
        var depthRange = MathF.Max(18f, planet.OrbitRadius * zoom * OrbitCalculator.InclinedOrbitTilt);
        return GetScreenDepth(position, depthCenter, depthRange);
    }

    private static float GetPlanetVisualZoom(CelestialBody planet, Vector2 position, Vector2 depthCenter, float zoom)
    {
        var depth = GetPlanetDepth(planet, position, depthCenter, zoom);
        return zoom * MathHelper.Lerp(0.88f, 1.13f, depth);
    }

    private void DrawSatelliteBodyWithDepth(NaturalSatellite satellite, Vector2 position, Vector2 depthCenter, Vector2 lightCenter, float zoom)
    {
        var depthRange = MathF.Max(8f, BodyPositionService.GetSatelliteOrbitVisualRadius(satellite, zoom) * OrbitCalculator.InclinedOrbitTilt);
        var depth = GetScreenDepth(position, depthCenter, depthRange);
        var visualZoom = zoom * MathHelper.Lerp(0.82f, 1.12f, depth);
        var color = Color.Lerp(ScaleColor(satellite.Color, 0.6f), ScaleColor(satellite.Color, 1.06f), depth);
        DrawToonPlanet(position, satellite.Radius, visualZoom, color, lightCenter, 0.72f);
    }

    private void DrawToonPlanet(
        Vector2 position,
        float radius,
        float zoom,
        Color baseColor,
        Vector2 lightCenter,
        float atmosphereIntensity)
    {
        var planetRadius = MathF.Max(1.5f, radius * zoom);
        var atmosphereScale = 1.42f;
        var atmosphereRadius = planetRadius * atmosphereScale;
        var destination = new Rectangle(
            (int)(position.X - atmosphereRadius),
            (int)(position.Y - atmosphereRadius),
            (int)(atmosphereRadius * 2f),
            (int)(atmosphereRadius * 2f));
        var lightDirection = GetInclinedLightDirection(position, lightCenter);

        if (lightDirection.LengthSquared() <= 0.0001f)
            lightDirection = new Vector3(0f, 0f, 1f);

        ConfigureToonPlanet(baseColor, lightDirection, 1f / atmosphereScale, atmosphereIntensity);

        _spriteBatch.End();
        _spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise,
            _shaders.ToonPlanet);
        _spriteBatch.Draw(_textures.Pixel, destination, Color.White);
        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
    }

    private void ConfigureToonPlanet(
        Color baseColor,
        Vector3 lightDirection,
        float planetRadius,
        float atmosphereIntensity)
    {
        lightDirection.Normalize();

        var lightColor = MixColor(ScaleColor(baseColor, 1.34f), new Color(255, 232, 170), 0.12f);
        var midLightColor = MixColor(ScaleColor(baseColor, 1.14f), new Color(255, 222, 160), 0.06f);
        var midShadowColor = MixColor(ScaleColor(baseColor, 0.66f), new Color(58, 59, 88), 0.08f);
        var shadowColor = MixColor(ScaleColor(baseColor, 0.42f), new Color(28, 31, 55), 0.16f);
        var atmosphereColor = ScaleColor(lightColor, 1.1f);
        var outlineColor = MixColor(shadowColor, SpaceColor, 0.32f);

        _shaders.ToonPlanet.Parameters["LightColor"]?.SetValue(lightColor.ToVector4());
        _shaders.ToonPlanet.Parameters["MidLightColor"]?.SetValue(midLightColor.ToVector4());
        _shaders.ToonPlanet.Parameters["BaseColor"]?.SetValue(baseColor.ToVector4());
        _shaders.ToonPlanet.Parameters["MidShadowColor"]?.SetValue(midShadowColor.ToVector4());
        _shaders.ToonPlanet.Parameters["ShadowColor"]?.SetValue(shadowColor.ToVector4());
        _shaders.ToonPlanet.Parameters["AtmosphereColor"]?.SetValue(atmosphereColor.ToVector4());
        _shaders.ToonPlanet.Parameters["OutlineColor"]?.SetValue(outlineColor.ToVector4());
        _shaders.ToonPlanet.Parameters["LightDirection"]?.SetValue(lightDirection);
        _shaders.ToonPlanet.Parameters["PlanetRadius"]?.SetValue(planetRadius);
        _shaders.ToonPlanet.Parameters["AtmosphereIntensity"]?.SetValue(atmosphereIntensity);
        _shaders.ToonPlanet.Parameters["LightThreshold"]?.SetValue(0.24f);
        _shaders.ToonPlanet.Parameters["ShadowThreshold"]?.SetValue(-0.18f);
        _shaders.ToonPlanet.Parameters["BandSoftness"]?.SetValue(0.035f);
        _shaders.ToonPlanet.Parameters["BandEdge0"]?.SetValue(0.56f);
        _shaders.ToonPlanet.Parameters["BandEdge1"]?.SetValue(0.68f);
        _shaders.ToonPlanet.Parameters["BandEdge2"]?.SetValue(0.82f);
        _shaders.ToonPlanet.Parameters["BandEdge3"]?.SetValue(0.94f);
        _shaders.ToonPlanet.Parameters["TextureOverlayStrength"]?.SetValue(0.11f);
        _shaders.ToonPlanet.Parameters["OutlineStrength"]?.SetValue(0.11f);
        _shaders.ToonPlanet.Parameters["NoiseScale"]?.SetValue(6.2f);
        _shaders.ToonPlanet.Parameters["Time"]?.SetValue(_shaderTime);
    }

    private static Vector3 GetInclinedLightDirection(Vector2 bodyPosition, Vector2 lightCenter)
    {
        var screenDirection = lightCenter - bodyPosition;
        var planeDepth = screenDirection.Y / OrbitCalculator.InclinedOrbitTilt;
        var lightDirection = new Vector3(screenDirection.X, 0f, planeDepth);

        if (lightDirection.LengthSquared() <= 0.0001f)
            return new Vector3(0f, 0f, 1f);

        lightDirection.Normalize();
        return lightDirection;
    }

    private static float GetScreenDepth(Vector2 position, Vector2 depthCenter, float depthRange)
    {
        var normalizedDepth = ((position.Y - depthCenter.Y) / depthRange + 1f) * 0.5f;
        return MathHelper.Clamp(normalizedDepth, 0f, 1f);
    }

    private static Color ScaleColor(Color color, float scale)
    {
        return new Color(
            (byte)MathHelper.Clamp(color.R * scale, 0f, 255f),
            (byte)MathHelper.Clamp(color.G * scale, 0f, 255f),
            (byte)MathHelper.Clamp(color.B * scale, 0f, 255f),
            color.A);
    }

    private static Color MixColor(Color from, Color to, float amount)
    {
        return Color.Lerp(from, to, MathHelper.Clamp(amount, 0f, 1f));
    }

    private void DrawRings(Vector2 position, float planetRadius, float zoom, float orbitTilt)
    {
        _primitives.DrawOrbit(position, planetRadius * 1.6f, zoom, orbitTilt, new Color(205, 185, 140, 180));
        _primitives.DrawOrbit(position, planetRadius * 2.05f, zoom, orbitTilt, new Color(173, 154, 115, 140));
    }

    private void DrawPerspectiveRings(Vector2 position, float planetRadius, float zoom, float orbitTilt, bool drawFront)
    {
        DrawSaturnRingShader(position, planetRadius, zoom, orbitTilt, drawFront);
    }

    private void DrawSaturnRingShader(Vector2 position, float planetRadius, float zoom, float orbitTilt, bool drawFront)
    {
        var outerRadius = planetRadius * zoom * 2.14f;
        var ringTilt = MathF.Max(0.12f, orbitTilt * 0.82f);
        var destination = new Rectangle(
            (int)(position.X - outerRadius),
            (int)(position.Y - outerRadius),
            (int)(outerRadius * 2f),
            (int)(outerRadius * 2f));

        ConfigureSaturnRings(ringTilt, 0.48f, 0.98f, drawFront, drawFront ? 0.78f : 0.48f);

        _spriteBatch.End();
        _spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise,
            _shaders.SaturnRings);
        _spriteBatch.Draw(_textures.Pixel, destination, Color.White);
        _spriteBatch.End();
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
    }

    private void ConfigureSaturnRings(float ringTilt, float innerRadius, float outerRadius, bool drawFront, float maxAlpha)
    {
        _shaders.SaturnRings.Parameters["RingTilt"]?.SetValue(ringTilt);
        _shaders.SaturnRings.Parameters["InnerRadius"]?.SetValue(innerRadius);
        _shaders.SaturnRings.Parameters["OuterRadius"]?.SetValue(outerRadius);
        _shaders.SaturnRings.Parameters["DrawFront"]?.SetValue(drawFront ? 1f : 0f);
        _shaders.SaturnRings.Parameters["MaxAlpha"]?.SetValue(maxAlpha);
    }

    private void DrawPerspectiveRing(
        Vector2 center,
        float radius,
        float zoom,
        float orbitTilt,
        bool drawFront,
        float maxAlpha,
        float maxThickness)
    {
        const int segments = 96;
        var previous = OrbitCalculator.GetOrbitPoint(center, radius, 0f, zoom, orbitTilt);

        for (var i = 1; i <= segments; i++)
        {
            var angle = MathHelper.TwoPi * i / segments;
            var previousAngle = MathHelper.TwoPi * (i - 1) / segments;
            var middleAngle = (previousAngle + angle) * 0.5f;
            var current = OrbitCalculator.GetOrbitPoint(center, radius, angle, zoom, orbitTilt);
            var midpoint = (previous + current) * 0.5f;

            if (IsInFrontOfSun(midpoint, center) == drawFront)
                DrawPerspectiveRingSegment(previous, current, middleAngle, maxAlpha, maxThickness);

            previous = current;
        }
    }

    private void DrawPerspectiveRingSegment(Vector2 start, Vector2 end, float angle, float maxAlpha, float maxThickness)
    {
        var frontDepth = (MathF.Sin(angle) + 1f) * 0.5f;
        var glow = MathF.Pow(frontDepth, 0.72f);
        var alpha = MathHelper.Lerp(0.16f, maxAlpha, glow);
        var thickness = MathHelper.Lerp(0.45f, maxThickness, glow);
        var color = Color.Lerp(new Color(130, 111, 76), new Color(255, 232, 164), glow);

        _primitives.DrawLine(start, end, WithAlpha(color, alpha * 0.24f), thickness + 1.45f);
        _primitives.DrawLine(start, end, WithAlpha(color, alpha), thickness);
    }

    private void DrawPerspectiveSatelliteOrbit(
        NaturalSatellite satellite,
        Vector2 center,
        float radius,
        float zoom,
        float orbitTilt,
        bool drawFront,
        Vector2 depthCenter)
    {
        const int segments = 112;
        var previous = OrbitCalculator.GetOrbitPoint(center, radius, 0f, zoom, orbitTilt);

        for (var i = 1; i <= segments; i++)
        {
            var angle = MathHelper.TwoPi * i / segments;
            var previousAngle = MathHelper.TwoPi * (i - 1) / segments;
            var middleAngle = (previousAngle + angle) * 0.5f;
            var current = OrbitCalculator.GetOrbitPoint(center, radius, angle, zoom, orbitTilt);
            var midpoint = (previous + current) * 0.5f;

            if (IsInFrontOfSun(midpoint, depthCenter) == drawFront)
                DrawPerspectiveSatelliteOrbitSegment(previous, current, middleAngle, satellite.Color);

            previous = current;
        }
    }

    private void DrawPerspectiveSatelliteOrbitSegment(Vector2 start, Vector2 end, float angle, Color baseColor)
    {
        var frontDepth = (MathF.Sin(angle) + 1f) * 0.5f;
        var glow = MathF.Pow(frontDepth, 0.72f);
        var alpha = MathHelper.Lerp(0.1f, 0.62f, glow);
        var thickness = MathHelper.Lerp(0.45f, 1.05f, glow);
        var color = Color.Lerp(ScaleColor(baseColor, 0.42f), ScaleColor(baseColor, 1.18f), glow);

        _primitives.DrawLine(start, end, WithAlpha(color, alpha * 0.22f), thickness + 1.35f);
        _primitives.DrawLine(start, end, WithAlpha(color, alpha), thickness);
    }

    private void DrawSelectionMarker(Vector2 position, float radius, float zoom)
    {
        var markerRadius = MathF.Max(16f, radius * zoom + 10f);
        _primitives.DrawCircleOutline(position, markerRadius, new Color(117, 220, 255, 230), 2f);
    }

    private void DrawCenterOfMass(SolarSystemState solarSystem, Vector2 center, float zoom)
    {
        if (solarSystem.ViewMode != SystemViewMode.TopDown)
            return;

        var position = center + solarSystem.GravitySimulation.GetCenterOfMass().ToRenderVector(zoom);
        var color = new Color(255, 82, 168);

        _primitives.DrawCircleOutline(position, 8f, color, 2f);
        _primitives.DrawLine(position + new Vector2(-13f, 0f), position + new Vector2(13f, 0f), color, 2f);
        _primitives.DrawLine(position + new Vector2(0f, -13f), position + new Vector2(0f, 13f), color, 2f);
    }
}

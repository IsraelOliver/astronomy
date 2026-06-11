using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Astronomia;

public sealed class StudyPanelRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly PrimitiveRenderer _primitives;
    private Language _currentLanguage = Language.Portuguese;

    public StudyPanelRenderer(SpriteBatch spriteBatch, SpriteFont font, PrimitiveRenderer primitives)
    {
        _spriteBatch = spriteBatch;
        _font = font;
        _primitives = primitives;
    }

    public void Draw(SolarSystemState solarSystem, Viewport viewport, Language language)
    {
        _currentLanguage = language;

        if (solarSystem.ViewMode == SystemViewMode.TopDown && solarSystem.SelectedPlanet is not null)
            DrawTopDownPlanetPanel(solarSystem, solarSystem.SelectedPlanet, viewport);
        else if (solarSystem.SelectedPlanet is not null)
            DrawPlanetPanel(solarSystem, solarSystem.SelectedPlanet, viewport);
        else if (solarSystem.IsSunSelected)
            DrawSunPanel(solarSystem.Sun, viewport);
    }

    public Rectangle GetPanelRectangle(SolarSystemState solarSystem, Viewport viewport, Language language)
    {
        var baseRectangle = new Rectangle(viewport.Width - 390, 72, 360, 304);

        if (!solarSystem.HasSelectedBody)
            return baseRectangle;

        if (solarSystem.ViewMode == SystemViewMode.TopDown && solarSystem.SelectedPlanet is not null)
            return GetTopDownPanelRectangle(viewport);

        if (solarSystem.SelectedPlanet is not null)
            return GetPlanetPanelRectangle(solarSystem, solarSystem.SelectedPlanet, viewport, language);

        return GetSunPanelRectangle(solarSystem.Sun, viewport, language);
    }

    public bool TryGetEditorSliderAt(SolarSystemState solarSystem, Viewport viewport, Vector2 point, out PlanetEditorField field, out double normalizedValue)
    {
        field = default;
        normalizedValue = 0d;

        if (solarSystem.ViewMode != SystemViewMode.TopDown || solarSystem.SelectedPlanet is null)
            return false;

        foreach (var candidate in GetSliderFields())
        {
            var hitRectangle = GetSliderHitRectangle(viewport, candidate.RowIndex);

            if (Contains(hitRectangle, point))
            {
                var track = GetSliderTrackRectangle(viewport, candidate.RowIndex);
                field = candidate.Field;
                normalizedValue = MathHelper.Clamp((point.X - track.Left) / (float)track.Width, 0f, 1f);
                return true;
            }
        }

        return false;
    }

    public void DrawHoverTooltip(
        SolarSystemState solarSystem,
        Viewport viewport,
        Vector2 mousePosition,
        Vector2 systemCenter,
        float zoom,
        Rectangle hudPanel,
        Rectangle languageButton,
        Rectangle languageMenu,
        bool isLanguageMenuOpen,
        Language language)
    {
        _currentLanguage = language;

        if (solarSystem.ViewMode != SystemViewMode.Inclined ||
            Contains(hudPanel, mousePosition) ||
            Contains(languageButton, mousePosition) ||
            (isLanguageMenuOpen && Contains(languageMenu, mousePosition)) ||
            (solarSystem.HasSelectedBody && Contains(GetPanelRectangle(solarSystem, viewport, language), mousePosition)))
            return;

        var planet = FindHoveredPlanet(solarSystem, systemCenter, zoom, mousePosition);

        if (planet is null)
            return;

        DrawPlanetTooltip(solarSystem, planet, mousePosition, viewport);
    }

    private void DrawTopDownPlanetPanel(SolarSystemState solarSystem, CelestialBody planet, Viewport viewport)
    {
        var panel = GetTopDownPanelRectangle(viewport);
        DrawPanelBackground(panel, planet.Color);

        var x = panel.X + 18;
        var y = panel.Y + 18;

        DrawText(GetBodyName(planet.Name), new Vector2(x, y), Color.White);
        y += 34;
        DrawSliderRow(viewport, 0, $"{T("panel.mass")}: {FormatScientific(solarSystem.GetPlanetMassKg(planet))} kg", solarSystem.GetEditorNormalizedValue(planet, PlanetEditorField.Mass), y, planet.Color);
        y += 52;
        DrawSliderRow(viewport, 1, $"{T("panel.rotation")}: {solarSystem.GetPlanetRotationSpeedKmh(planet):N0} km/h", solarSystem.GetEditorNormalizedValue(planet, PlanetEditorField.Rotation), y, planet.Color);
        y += 52;
        DrawSliderRow(viewport, 2, $"{T("panel.translation")}: {solarSystem.GetPlanetTranslationSpeedKms(planet):0.##} km/s", solarSystem.GetEditorNormalizedValue(planet, PlanetEditorField.Translation), y, planet.Color);
        y += 48;
        DrawDiagnostics(solarSystem, planet, new Vector2(x, y));
    }

    private void DrawPlanetPanel(SolarSystemState solarSystem, CelestialBody planet, Viewport viewport)
    {
        var panel = GetPlanetPanelRectangle(solarSystem, planet, viewport, _currentLanguage);
        var contentWidth = panel.Width - 36;
        var summary = AstronomyTextCatalog.GetPlanetSummary(_currentLanguage, planet);
        var summaryLines = WrapText(summary, contentWidth);

        DrawPanelBackground(panel, planet.Color);

        var x = panel.X + 18;
        var y = panel.Y + 18;

        y = DrawWrappedLine(GetBodyName(planet.Name), new Vector2(x, y), Color.White, contentWidth, 30);
        y += 4;
        y = DrawWrappedLine($"{T("panel.averageDistance")}: {planet.DistanceAu:0.00} UA", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.orbitalPeriod")}: {FormatOrbitalPeriod(planet.OrbitalPeriodDays, _currentLanguage)}", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.diameter")}: {planet.DiameterKm:N0} km", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.mass")}: {FormatScientific(planet.MassKg)} kg", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.relativeMass")}: {planet.MassEarths:0.###} x {T("panel.earth")}", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.rotation")}: {planet.RotationSpeedKmh:N0} km/h", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.translation")}: {planet.OrbitalSpeedKms:0.##} km/s", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        var phase = GetVisiblePhase(solarSystem, planet);
        y = DrawWrappedLine($"{T("panel.visiblePhase")}: {phase.Name} ({phase.IlluminationPercent:0}%)", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y += 8;
        y = DrawInclinedContext(solarSystem, planet, new Vector2(x, y), contentWidth);
        y += 10;
        y = DrawWrappedText(summaryLines, new Vector2(x, y), new Color(171, 196, 232), 23);
        y += 12;
        y = DrawWrappedLine(T("panel.cameraFollowingOrbit"), new Vector2(x, y), new Color(109, 225, 166), contentWidth, 23);
        DrawWrappedLine(T("panel.freeCameraHint"), new Vector2(x, y), new Color(167, 185, 215), contentWidth, 23);
    }

    private void DrawSunPanel(SolarBody sun, Viewport viewport)
    {
        var panel = GetSunPanelRectangle(sun, viewport, _currentLanguage);
        var contentWidth = panel.Width - 36;
        var sunType = AstronomyTextCatalog.GetSunType(_currentLanguage, sun);
        var summary = AstronomyTextCatalog.GetSunSummary(_currentLanguage, sun);
        var summaryLines = WrapText(summary, contentWidth);

        DrawPanelBackground(panel, sun.Color);

        var x = panel.X + 18;
        var y = panel.Y + 18;

        y = DrawWrappedLine(GetBodyName(sun.Name), new Vector2(x, y), Color.White, contentWidth, 30);
        y += 4;
        y = DrawWrappedLine($"{T("panel.type")}: {sunType}", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.position")}: {T("panel.solarSystemCenter")}", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.diameter")}: {sun.DiameterKm:N0} km", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.mass")}: {sun.MassEarths:N0} x {T("panel.earth")}", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.surfaceGravity")}: {sun.GravityMs2:0} m/s2", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"{T("panel.surfaceTemperature")}: {sun.SurfaceTemperatureC:N0} C", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y += 10;
        y = DrawWrappedText(summaryLines, new Vector2(x, y), new Color(171, 196, 232), 23);
        y += 12;
        y = DrawWrappedLine(T("panel.cameraFocusedSun"), new Vector2(x, y), new Color(109, 225, 166), contentWidth, 23);
        DrawWrappedLine(T("panel.freeCameraHint"), new Vector2(x, y), new Color(167, 185, 215), contentWidth, 23);
    }

    private Rectangle GetPlanetPanelRectangle(SolarSystemState solarSystem, CelestialBody planet, Viewport viewport, Language language)
    {
        var rectangle = new Rectangle(viewport.Width - 390, 72, 360, 304);
        var contentWidth = rectangle.Width - 36;
        _currentLanguage = language;
        var phase = GetVisiblePhase(solarSystem, planet);
        var summary = AstronomyTextCatalog.GetPlanetSummary(language, planet);
        var lines = new List<string>
        {
            GetBodyName(planet.Name),
            $"{T("panel.averageDistance")}: {planet.DistanceAu:0.00} UA",
            $"{T("panel.orbitalPeriod")}: {FormatOrbitalPeriod(planet.OrbitalPeriodDays, language)}",
            $"{T("panel.diameter")}: {planet.DiameterKm:N0} km",
            $"{T("panel.mass")}: {FormatScientific(planet.MassKg)} kg",
            $"{T("panel.relativeMass")}: {planet.MassEarths:0.###} x {T("panel.earth")}",
            $"{T("panel.rotation")}: {planet.RotationSpeedKmh:N0} km/h",
            $"{T("panel.translation")}: {planet.OrbitalSpeedKms:0.##} km/s",
            $"{T("panel.visiblePhase")}: {phase.Name} ({phase.IlluminationPercent:0}%)",
            T("panel.orbitalContext"),
            $"{T("panel.group")}: {GetPlanetGroup(planet, language)}",
            $"{T("panel.currentDistance")}: {GetCurrentDistanceAu(solarSystem, planet):0.00} UA",
            $"{T("panel.visualPosition")}: {GetVisualDepthText(solarSystem, planet, language)}",
            $"{T("panel.eccentricity")}: {planet.Eccentricity:0.000}",
            $"{T("panel.orbitalInclination")}: {planet.OrbitPlaneTiltDegrees:0.0} {T("panel.degrees")}",
            summary,
            T("panel.cameraFollowingOrbit"),
            T("panel.freeCameraHint")
        };
        rectangle.Height = Math.Min(MeasurePanelHeight(lines, contentWidth), viewport.Height - rectangle.Y - 18);
        return rectangle;
    }

    private Rectangle GetSunPanelRectangle(SolarBody sun, Viewport viewport, Language language)
    {
        var rectangle = new Rectangle(viewport.Width - 390, 72, 360, 304);
        var contentWidth = rectangle.Width - 36;
        _currentLanguage = language;
        var sunType = AstronomyTextCatalog.GetSunType(language, sun);
        var summary = AstronomyTextCatalog.GetSunSummary(language, sun);
        var lines = new List<string>
        {
            GetBodyName(sun.Name),
            $"{T("panel.type")}: {sunType}",
            $"{T("panel.position")}: {T("panel.solarSystemCenter")}",
            $"{T("panel.diameter")}: {sun.DiameterKm:N0} km",
            $"{T("panel.mass")}: {sun.MassEarths:N0} x {T("panel.earth")}",
            $"{T("panel.surfaceGravity")}: {sun.GravityMs2:0} m/s2",
            $"{T("panel.surfaceTemperature")}: {sun.SurfaceTemperatureC:N0} C",
            summary,
            T("panel.cameraFocusedSun"),
            T("panel.freeCameraHint")
        };
        rectangle.Height = Math.Min(MeasurePanelHeight(lines, contentWidth), viewport.Height - rectangle.Y - 18);
        return rectangle;
    }

    private static Rectangle GetTopDownPanelRectangle(Viewport viewport)
    {
        return new Rectangle(viewport.Width - 430, 72, 400, 384);
    }

    private void DrawPanelBackground(Rectangle panel, Color borderColor)
    {
        _primitives.FillRectangle(panel, new Color(8, 14, 28, 228));
        _primitives.DrawRectangle(panel, borderColor, 2);
    }

    private void DrawSliderRow(Viewport viewport, int rowIndex, string label, double normalizedValue, int y, Color accentColor)
    {
        var panel = GetTopDownPanelRectangle(viewport);
        var track = GetSliderTrackRectangle(viewport, rowIndex);
        var fill = new Rectangle(track.X, track.Y, (int)(track.Width * normalizedValue), track.Height);
        var knobX = track.X + (int)(track.Width * normalizedValue);
        var knob = new Rectangle(knobX - 5, track.Y - 6, 10, 20);

        DrawText(label, new Vector2(panel.X + 18, y), new Color(216, 226, 242));
        _primitives.FillRectangle(track, new Color(20, 34, 54, 235));
        _primitives.FillRectangle(fill, new Color((int)accentColor.R, (int)accentColor.G, (int)accentColor.B, 185));
        _primitives.DrawRectangle(track, new Color(91, 122, 168, 220), 1);
        _primitives.FillRectangle(knob, Color.White);
    }

    private void DrawDiagnostics(SolarSystemState solarSystem, CelestialBody planet, Vector2 position)
    {
        var diagnostics = solarSystem.GetPlanetDiagnostics(planet);
        if (diagnostics is null)
            return;

        var y = (int)position.Y;
        DrawText(T("panel.currentReading"), new Vector2(position.X, y), Color.White);
        y += 26;
        DrawText($"{T("panel.currentSpeed")}: {diagnostics.CurrentSpeedKms:0.##} km/s", new Vector2(position.X, y), new Color(190, 211, 240));
        y += 22;
        DrawText($"{T("panel.acceleration")}: {FormatScientific(diagnostics.AccelerationMs2)} m/s2", new Vector2(position.X, y), new Color(190, 211, 240));
        y += 22;
        DrawText($"{T("panel.gravitationalForce")}: {FormatScientific(diagnostics.GravitationalForceN)} N", new Vector2(position.X, y), new Color(190, 211, 240));
        y += 22;
        DrawText($"{T("panel.centerMassDistance")}: {FormatAstronomicalDistance(diagnostics.DistanceFromCenterOfMassMeters)}", new Vector2(position.X, y), new Color(190, 211, 240));
        y += 22;
        DrawText($"{T("panel.orbitalEnergy")}: {FormatScientific(diagnostics.SimplifiedOrbitalEnergyJ)} J", new Vector2(position.X, y), new Color(190, 211, 240));
    }

    private int DrawInclinedContext(SolarSystemState solarSystem, CelestialBody planet, Vector2 position, float maxWidth)
    {
        var y = (int)position.Y;
        var contextColor = new Color(190, 211, 240);

        y = DrawWrappedLine(T("panel.orbitalContext"), new Vector2(position.X, y), Color.White, maxWidth, 25);
        y = DrawWrappedLine($"{T("panel.group")}: {GetPlanetGroup(planet, _currentLanguage)}", new Vector2(position.X, y), contextColor, maxWidth, 22);
        y = DrawWrappedLine($"{T("panel.currentDistance")}: {GetCurrentDistanceAu(solarSystem, planet):0.00} UA", new Vector2(position.X, y), contextColor, maxWidth, 22);
        y = DrawWrappedLine($"{T("panel.visualPosition")}: {GetVisualDepthText(solarSystem, planet, _currentLanguage)}", new Vector2(position.X, y), contextColor, maxWidth, 22);
        y = DrawWrappedLine($"{T("panel.eccentricity")}: {planet.Eccentricity:0.000}", new Vector2(position.X, y), contextColor, maxWidth, 22);
        y = DrawWrappedLine($"{T("panel.orbitalInclination")}: {planet.OrbitPlaneTiltDegrees:0.0} {T("panel.degrees")}", new Vector2(position.X, y), contextColor, maxWidth, 22);

        return y;
    }

    private CelestialBody? FindHoveredPlanet(SolarSystemState solarSystem, Vector2 systemCenter, float zoom, Vector2 mousePosition)
    {
        CelestialBody? closest = null;
        var closestDistance = float.MaxValue;

        foreach (var planet in solarSystem.Planets)
        {
            var position = BodyPositionService.GetPlanetPosition(solarSystem, systemCenter, planet, zoom);
            var hitRadius = MathF.Max(14f, planet.Radius * zoom + 10f);

            if (planet.HasRings)
                hitRadius = MathF.Max(hitRadius, planet.Radius * zoom * 2.2f + 6f);

            var distance = Vector2.Distance(mousePosition, position);

            if (distance <= hitRadius && distance < closestDistance)
            {
                closest = planet;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void DrawPlanetTooltip(SolarSystemState solarSystem, CelestialBody planet, Vector2 mousePosition, Viewport viewport)
    {
        var offset = OrbitCalculator.GetOrbitOffset(planet, solarSystem.SimulationDays, 1f, OrbitCalculator.InclinedOrbitTilt);
        var distanceAu = offset.Length() / PhysicsConstants.PixelsPerAstronomicalUnit;
        var lines = new[]
        {
            GetBodyName(planet.Name),
            $"{T("panel.currentDistance")}: {distanceAu:0.00} UA",
            $"{T("panel.orbitalSpeed")}: {planet.OrbitalSpeedKms:0.##} km/s"
        };

        var width = 0f;
        foreach (var line in lines)
            width = MathF.Max(width, _font.MeasureString(line).X);

        var tooltip = new Rectangle(
            (int)MathHelper.Clamp(mousePosition.X + 18f, 8f, viewport.Width - width - 34f),
            (int)MathHelper.Clamp(mousePosition.Y + 18f, 8f, viewport.Height - 96f),
            (int)width + 24,
            88);

        _primitives.FillRectangle(tooltip, new Color(8, 14, 28, 235));
        _primitives.DrawRectangle(tooltip, ScaleColor(planet.Color, 1.22f), 1);

        var x = tooltip.X + 12;
        var y = tooltip.Y + 10;
        DrawText(lines[0], new Vector2(x, y), Color.White);
        y += 25;
        DrawText(lines[1], new Vector2(x, y), new Color(216, 226, 242));
        y += 22;
        DrawText(lines[2], new Vector2(x, y), new Color(190, 211, 240));
    }

    private int DrawWrappedText(IReadOnlyList<string> lines, Vector2 position, Color color, int lineHeight)
    {
        var y = (int)position.Y;

        foreach (var line in lines)
        {
            DrawText(line, new Vector2(position.X, y), color);
            y += lineHeight;
        }

        return y;
    }

    private int DrawWrappedLine(string text, Vector2 position, Color color, float maxWidth, int lineHeight)
    {
        return DrawWrappedText(WrapText(text, maxWidth), position, color, lineHeight);
    }

    private IReadOnlyList<string> WrapText(string text, float maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";

            if (_font.MeasureString(candidate).X <= maxWidth)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);

            currentLine = word;
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines.Count > 0 ? lines : new[] { string.Empty };
    }

    private int MeasurePanelHeight(IReadOnlyList<string> lines, float maxWidth)
    {
        var height = 36;

        foreach (var line in lines)
            height += Math.Max(1, WrapText(line, maxWidth).Count) * 23;

        return height + 26;
    }

    private static string GetPlanetGroup(CelestialBody planet, Language language)
    {
        if (planet.Name is "Mercurio" or "Venus" or "Terra" or "Marte")
            return TextCatalog.Get(language, "group.rocky");

        if (planet.Name is "Jupiter" or "Saturno")
            return TextCatalog.Get(language, "group.gasGiant");

        if (planet.Name is "Urano" or "Netuno")
            return TextCatalog.Get(language, "group.iceGiant");

        if (planet.Name == "Plutao")
            return TextCatalog.Get(language, "group.dwarfKuiper");

        return TextCatalog.Get(language, "group.orbitalBody");
    }

    private static float GetCurrentDistanceAu(SolarSystemState solarSystem, CelestialBody planet)
    {
        var offset = OrbitCalculator.GetOrbitOffset(planet, solarSystem.SimulationDays, 1f, OrbitCalculator.InclinedOrbitTilt);
        return offset.Length() / (float)PhysicsConstants.PixelsPerAstronomicalUnit;
    }

    private static string GetVisualDepthText(SolarSystemState solarSystem, CelestialBody planet, Language language)
    {
        var offset = OrbitCalculator.GetOrbitOffset(planet, solarSystem.SimulationDays, 1f, OrbitCalculator.InclinedOrbitTilt);
        return offset.Y >= 0f
            ? TextCatalog.Get(language, "depth.frontSun")
            : TextCatalog.Get(language, "depth.behindSun");
    }

    private (string Name, float IlluminationPercent) GetVisiblePhase(SolarSystemState solarSystem, CelestialBody planet)
    {
        var offset = OrbitCalculator.GetOrbitOffset(planet, solarSystem.SimulationDays, 1f, OrbitCalculator.InclinedOrbitTilt);
        var lightDirection = new Vector3(-offset.X, 0f, -offset.Y / OrbitCalculator.InclinedOrbitTilt);

        if (lightDirection.LengthSquared() <= 0.0001f)
            return (T("phase.full"), 100f);

        lightDirection.Normalize();

        var visibleAmount = MathHelper.Clamp((lightDirection.Z + 1f) * 0.5f, 0f, 1f);
        var name = visibleAmount switch
        {
            < 0.12f => T("phase.new"),
            < 0.38f => T("phase.thinCrescent"),
            < 0.62f => T("phase.half"),
            < 0.88f => T("phase.gibbous"),
            _ => T("phase.full")
        };

        return (name, visibleAmount * 100f);
    }

    private static string FormatScientific(double value)
    {
        if (System.Math.Abs(value) < double.Epsilon)
            return "0";

        var sign = value < 0d ? "-" : string.Empty;
        var absoluteValue = System.Math.Abs(value);
        var exponent = (int)Math.Floor(Math.Log10(absoluteValue));
        var mantissa = absoluteValue / Math.Pow(10, exponent);
        return $"{sign}{mantissa:0.###} x10^{exponent}";
    }

    private static string FormatAstronomicalDistance(double meters)
    {
        var au = meters / PhysicsConstants.AstronomicalUnitMeters;

        if (au >= 0.01d)
            return $"{au:0.###} UA";

        return $"{meters / 1_000d:N0} km";
    }

    private static string FormatOrbitalPeriod(float days, Language language)
    {
        if (days < 730f)
            return $"{days:0} {TextCatalog.Get(language, "period.days")}";

        return $"{days / 365.25f:0.0} {TextCatalog.Get(language, "period.years")}";
    }

    private static Color ScaleColor(Color color, float scale)
    {
        return new Color(
            (byte)MathHelper.Clamp(color.R * scale, 0f, 255f),
            (byte)MathHelper.Clamp(color.G * scale, 0f, 255f),
            (byte)MathHelper.Clamp(color.B * scale, 0f, 255f),
            color.A);
    }

    private void DrawText(string text, Vector2 position, Color color)
    {
        _spriteBatch.DrawString(_font, text, position, color);
    }

    private string T(string key)
    {
        return TextCatalog.Get(_currentLanguage, key);
    }

    private string GetBodyName(string bodyName)
    {
        return AstronomyTextCatalog.GetBodyName(_currentLanguage, bodyName);
    }

    private static Rectangle GetSliderTrackRectangle(Viewport viewport, int rowIndex)
    {
        var panel = GetTopDownPanelRectangle(viewport);
        return new Rectangle(panel.X + 18, panel.Y + 72 + rowIndex * 52, panel.Width - 36, 8);
    }

    private static Rectangle GetSliderHitRectangle(Viewport viewport, int rowIndex)
    {
        var track = GetSliderTrackRectangle(viewport, rowIndex);
        return new Rectangle(track.X, track.Y - 12, track.Width, 32);
    }

    private static IReadOnlyList<(PlanetEditorField Field, int RowIndex)> GetSliderFields()
    {
        return new[]
        {
            (PlanetEditorField.Mass, 0),
            (PlanetEditorField.Rotation, 1),
            (PlanetEditorField.Translation, 2)
        };
    }

    private static bool Contains(Rectangle rectangle, Vector2 point)
    {
        return point.X >= rectangle.Left &&
            point.X <= rectangle.Right &&
            point.Y >= rectangle.Top &&
            point.Y <= rectangle.Bottom;
    }
}

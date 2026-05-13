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

    public StudyPanelRenderer(SpriteBatch spriteBatch, SpriteFont font, PrimitiveRenderer primitives)
    {
        _spriteBatch = spriteBatch;
        _font = font;
        _primitives = primitives;
    }

    public void Draw(SolarSystemState solarSystem, Viewport viewport)
    {
        if (solarSystem.ViewMode == SystemViewMode.TopDown && solarSystem.SelectedPlanet is not null)
            DrawTopDownPlanetPanel(solarSystem, solarSystem.SelectedPlanet, viewport);
        else if (solarSystem.SelectedPlanet is not null)
            DrawPlanetPanel(solarSystem, solarSystem.SelectedPlanet, viewport);
        else if (solarSystem.IsSunSelected)
            DrawSunPanel(solarSystem.Sun, viewport);
    }

    public Rectangle GetPanelRectangle(SolarSystemState solarSystem, Viewport viewport)
    {
        var baseRectangle = new Rectangle(viewport.Width - 390, 72, 360, 304);

        if (!solarSystem.HasSelectedBody)
            return baseRectangle;

        if (solarSystem.ViewMode == SystemViewMode.TopDown && solarSystem.SelectedPlanet is not null)
            return GetTopDownPanelRectangle(viewport);

        if (solarSystem.SelectedPlanet is not null)
            return GetPlanetPanelRectangle(solarSystem, solarSystem.SelectedPlanet, viewport);

        return GetSunPanelRectangle(solarSystem.Sun, viewport);
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
        Rectangle viewModeButton)
    {
        if (solarSystem.ViewMode != SystemViewMode.Inclined ||
            Contains(hudPanel, mousePosition) ||
            Contains(viewModeButton, mousePosition) ||
            (solarSystem.HasSelectedBody && Contains(GetPanelRectangle(solarSystem, viewport), mousePosition)))
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

        DrawText(planet.Name, new Vector2(x, y), Color.White);
        y += 34;
        DrawSliderRow(viewport, 0, $"Massa: {FormatScientific(solarSystem.GetPlanetMassKg(planet))} kg", solarSystem.GetEditorNormalizedValue(planet, PlanetEditorField.Mass), y, planet.Color);
        y += 52;
        DrawSliderRow(viewport, 1, $"Rotacao: {solarSystem.GetPlanetRotationSpeedKmh(planet):N0} km/h", solarSystem.GetEditorNormalizedValue(planet, PlanetEditorField.Rotation), y, planet.Color);
        y += 52;
        DrawSliderRow(viewport, 2, $"Translacao: {solarSystem.GetPlanetTranslationSpeedKms(planet):0.##} km/s", solarSystem.GetEditorNormalizedValue(planet, PlanetEditorField.Translation), y, planet.Color);
        y += 48;
        DrawDiagnostics(solarSystem, planet, new Vector2(x, y));
    }

    private void DrawPlanetPanel(SolarSystemState solarSystem, CelestialBody planet, Viewport viewport)
    {
        var panel = GetPlanetPanelRectangle(solarSystem, planet, viewport);
        var contentWidth = panel.Width - 36;
        var summaryLines = WrapText(planet.Summary, contentWidth);

        DrawPanelBackground(panel, planet.Color);

        var x = panel.X + 18;
        var y = panel.Y + 18;

        y = DrawWrappedLine(planet.Name, new Vector2(x, y), Color.White, contentWidth, 30);
        y += 4;
        y = DrawWrappedLine($"Distancia media: {planet.DistanceAu:0.00} UA", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Periodo orbital: {FormatOrbitalPeriod(planet.OrbitalPeriodDays)}", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Diametro: {planet.DiameterKm:N0} km", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Massa: {FormatScientific(planet.MassKg)} kg", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Massa relativa: {planet.MassEarths:0.###} x Terra", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Rotacao: {planet.RotationSpeedKmh:N0} km/h", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Translacao: {planet.OrbitalSpeedKms:0.##} km/s", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        var phase = GetVisiblePhase(solarSystem, planet);
        y = DrawWrappedLine($"Fase visivel: {phase.Name} ({phase.IlluminationPercent:0}%)", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y += 8;
        y = DrawInclinedContext(solarSystem, planet, new Vector2(x, y), contentWidth);
        y += 10;
        y = DrawWrappedText(summaryLines, new Vector2(x, y), new Color(171, 196, 232), 23);
        y += 12;
        y = DrawWrappedLine("Camera acompanhando a orbita", new Vector2(x, y), new Color(109, 225, 166), contentWidth, 23);
        DrawWrappedLine("C para voltar ao modo livre", new Vector2(x, y), new Color(167, 185, 215), contentWidth, 23);
    }

    private void DrawSunPanel(SolarBody sun, Viewport viewport)
    {
        var panel = GetSunPanelRectangle(sun, viewport);
        var contentWidth = panel.Width - 36;
        var summaryLines = WrapText(sun.Summary, contentWidth);

        DrawPanelBackground(panel, sun.Color);

        var x = panel.X + 18;
        var y = panel.Y + 18;

        y = DrawWrappedLine(sun.Name, new Vector2(x, y), Color.White, contentWidth, 30);
        y += 4;
        y = DrawWrappedLine($"Tipo: {sun.Type}", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine("Posicao: centro do Sistema Solar", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Diametro: {sun.DiameterKm:N0} km", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Massa: {sun.MassEarths:N0} x Terra", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Gravidade sup.: {sun.GravityMs2:0} m/s2", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y = DrawWrappedLine($"Temp. superficie: {sun.SurfaceTemperatureC:N0} C", new Vector2(x, y), new Color(216, 226, 242), contentWidth, 23);
        y += 10;
        y = DrawWrappedText(summaryLines, new Vector2(x, y), new Color(171, 196, 232), 23);
        y += 12;
        y = DrawWrappedLine("Camera focada no Sol", new Vector2(x, y), new Color(109, 225, 166), contentWidth, 23);
        DrawWrappedLine("C para voltar ao modo livre", new Vector2(x, y), new Color(167, 185, 215), contentWidth, 23);
    }

    private Rectangle GetPlanetPanelRectangle(SolarSystemState solarSystem, CelestialBody planet, Viewport viewport)
    {
        var rectangle = new Rectangle(viewport.Width - 390, 72, 360, 304);
        var contentWidth = rectangle.Width - 36;
        var phase = GetVisiblePhase(solarSystem, planet);
        var lines = new List<string>
        {
            planet.Name,
            $"Distancia media: {planet.DistanceAu:0.00} UA",
            $"Periodo orbital: {FormatOrbitalPeriod(planet.OrbitalPeriodDays)}",
            $"Diametro: {planet.DiameterKm:N0} km",
            $"Massa: {FormatScientific(planet.MassKg)} kg",
            $"Massa relativa: {planet.MassEarths:0.###} x Terra",
            $"Rotacao: {planet.RotationSpeedKmh:N0} km/h",
            $"Translacao: {planet.OrbitalSpeedKms:0.##} km/s",
            $"Fase visivel: {phase.Name} ({phase.IlluminationPercent:0}%)",
            "Contexto orbital",
            $"Grupo: {GetPlanetGroup(planet)}",
            $"Distancia atual: {GetCurrentDistanceAu(solarSystem, planet):0.00} UA",
            $"Posicao visual: {GetVisualDepthText(solarSystem, planet)}",
            $"Excentricidade: {planet.Eccentricity:0.000}",
            $"Inclinacao orbital: {planet.OrbitPlaneTiltDegrees:0.0} graus",
            planet.Summary,
            "Camera acompanhando a orbita",
            "C para voltar ao modo livre"
        };
        rectangle.Height = Math.Min(MeasurePanelHeight(lines, contentWidth), viewport.Height - rectangle.Y - 18);
        return rectangle;
    }

    private Rectangle GetSunPanelRectangle(SolarBody sun, Viewport viewport)
    {
        var rectangle = new Rectangle(viewport.Width - 390, 72, 360, 304);
        var contentWidth = rectangle.Width - 36;
        var lines = new List<string>
        {
            sun.Name,
            $"Tipo: {sun.Type}",
            "Posicao: centro do Sistema Solar",
            $"Diametro: {sun.DiameterKm:N0} km",
            $"Massa: {sun.MassEarths:N0} x Terra",
            $"Gravidade sup.: {sun.GravityMs2:0} m/s2",
            $"Temp. superficie: {sun.SurfaceTemperatureC:N0} C",
            sun.Summary,
            "Camera focada no Sol",
            "C para voltar ao modo livre"
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
        DrawText("Leitura atual", new Vector2(position.X, y), Color.White);
        y += 26;
        DrawText($"Velocidade atual: {diagnostics.CurrentSpeedKms:0.##} km/s", new Vector2(position.X, y), new Color(190, 211, 240));
        y += 22;
        DrawText($"Aceleracao: {FormatScientific(diagnostics.AccelerationMs2)} m/s2", new Vector2(position.X, y), new Color(190, 211, 240));
        y += 22;
        DrawText($"Forca gravitacional: {FormatScientific(diagnostics.GravitationalForceN)} N", new Vector2(position.X, y), new Color(190, 211, 240));
        y += 22;
        DrawText($"Dist. centro massa: {FormatAstronomicalDistance(diagnostics.DistanceFromCenterOfMassMeters)}", new Vector2(position.X, y), new Color(190, 211, 240));
        y += 22;
        DrawText($"Energia orbital: {FormatScientific(diagnostics.SimplifiedOrbitalEnergyJ)} J", new Vector2(position.X, y), new Color(190, 211, 240));
    }

    private int DrawInclinedContext(SolarSystemState solarSystem, CelestialBody planet, Vector2 position, float maxWidth)
    {
        var y = (int)position.Y;
        var contextColor = new Color(190, 211, 240);

        y = DrawWrappedLine("Contexto orbital", new Vector2(position.X, y), Color.White, maxWidth, 25);
        y = DrawWrappedLine($"Grupo: {GetPlanetGroup(planet)}", new Vector2(position.X, y), contextColor, maxWidth, 22);
        y = DrawWrappedLine($"Distancia atual: {GetCurrentDistanceAu(solarSystem, planet):0.00} UA", new Vector2(position.X, y), contextColor, maxWidth, 22);
        y = DrawWrappedLine($"Posicao visual: {GetVisualDepthText(solarSystem, planet)}", new Vector2(position.X, y), contextColor, maxWidth, 22);
        y = DrawWrappedLine($"Excentricidade: {planet.Eccentricity:0.000}", new Vector2(position.X, y), contextColor, maxWidth, 22);
        y = DrawWrappedLine($"Inclinacao orbital: {planet.OrbitPlaneTiltDegrees:0.0} graus", new Vector2(position.X, y), contextColor, maxWidth, 22);

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
            planet.Name,
            $"Distancia atual: {distanceAu:0.00} UA",
            $"Velocidade orbital: {planet.OrbitalSpeedKms:0.##} km/s"
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

    private static string GetPlanetGroup(CelestialBody planet)
    {
        if (planet.Name is "Mercurio" or "Venus" or "Terra" or "Marte")
            return "rochoso";

        if (planet.Name is "Jupiter" or "Saturno")
            return "gigante gasoso";

        if (planet.Name is "Urano" or "Netuno")
            return "gigante gelado";

        if (planet.Name == "Plutao")
            return "planeta anao / Kuiper";

        return "corpo orbital";
    }

    private static float GetCurrentDistanceAu(SolarSystemState solarSystem, CelestialBody planet)
    {
        var offset = OrbitCalculator.GetOrbitOffset(planet, solarSystem.SimulationDays, 1f, OrbitCalculator.InclinedOrbitTilt);
        return offset.Length() / (float)PhysicsConstants.PixelsPerAstronomicalUnit;
    }

    private static string GetVisualDepthText(SolarSystemState solarSystem, CelestialBody planet)
    {
        var offset = OrbitCalculator.GetOrbitOffset(planet, solarSystem.SimulationDays, 1f, OrbitCalculator.InclinedOrbitTilt);
        return offset.Y >= 0f ? "frente do Sol" : "atras do Sol";
    }

    private static (string Name, float IlluminationPercent) GetVisiblePhase(SolarSystemState solarSystem, CelestialBody planet)
    {
        var offset = OrbitCalculator.GetOrbitOffset(planet, solarSystem.SimulationDays, 1f, OrbitCalculator.InclinedOrbitTilt);
        var lightDirection = new Vector3(-offset.X, 0f, -offset.Y / OrbitCalculator.InclinedOrbitTilt);

        if (lightDirection.LengthSquared() <= 0.0001f)
            return ("cheia", 100f);

        lightDirection.Normalize();

        var visibleAmount = MathHelper.Clamp((lightDirection.Z + 1f) * 0.5f, 0f, 1f);
        var name = visibleAmount switch
        {
            < 0.12f => "nova",
            < 0.38f => "crescente fina",
            < 0.62f => "meia fase",
            < 0.88f => "gibosa",
            _ => "cheia"
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

    private static string FormatOrbitalPeriod(float days)
    {
        if (days < 730f)
            return $"{days:0} dias";

        return $"{days / 365.25f:0.0} anos";
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

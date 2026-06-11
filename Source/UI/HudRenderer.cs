using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Astronomia;

public sealed class HudRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly PrimitiveRenderer _primitives;

    public HudRenderer(SpriteBatch spriteBatch, SpriteFont font, PrimitiveRenderer primitives)
    {
        _spriteBatch = spriteBatch;
        _font = font;
        _primitives = primitives;
    }

    public Rectangle PanelRectangle => new(18, 18, 350, 124);

    public Rectangle GetSunCenterButtonRectangle(Viewport viewport)
    {
        return new Rectangle(22, viewport.Height - 72, 168, 50);
    }

    public Rectangle GetFilterButtonRectangle(Viewport viewport)
    {
        var sunButton = GetSunCenterButtonRectangle(viewport);
        return new Rectangle(sunButton.Right + 10, sunButton.Y, 104, 50);
    }

    public Rectangle GetFilterMenuRectangle(Viewport viewport)
    {
        var button = GetFilterButtonRectangle(viewport);
        return new Rectangle(button.X, button.Y - 86, 226, 76);
    }

    public Rectangle GetCenterOfMassCheckboxRectangle(Viewport viewport)
    {
        var menu = GetFilterMenuRectangle(viewport);
        return new Rectangle(menu.X + 14, menu.Y + 38, 18, 18);
    }

    public void Draw(SolarSystemState solarSystem, float zoom, Viewport viewport)
    {
        var panel = PanelRectangle;
        _primitives.FillRectangle(panel, new Color(8, 14, 28, 215));
        _primitives.DrawRectangle(panel, new Color(78, 105, 150, 210), 1);

        var y = 32;
        DrawText("Simulador de Astronomia", new Vector2(34, y), Color.White);
        y += 30;
        DrawText($"Tempo: dia {solarSystem.SimulationDays:0.0}", new Vector2(34, y), new Color(210, 222, 240));
        y += 23;
        DrawText($"Escala: {solarSystem.DaysPerSecond:0.##} dias / segundo", new Vector2(34, y), new Color(210, 222, 240));
        y += 23;
        DrawText($"Zoom: {zoom:0.00}x", new Vector2(34, y), new Color(210, 222, 240));

        var state = solarSystem.Paused ? "PAUSADO" : "RODANDO";
        var stateColor = solarSystem.Paused ? new Color(255, 182, 90) : new Color(91, 216, 144);
        DrawText(state, new Vector2(viewport.Width - 124, 24), stateColor);

        if (solarSystem.ViewMode == SystemViewMode.TopDown)
        {
            DrawSunCenterButton(viewport);
            DrawFilterButton(solarSystem, viewport);

            if (solarSystem.IsFilterMenuOpen)
                DrawFilterMenu(solarSystem, viewport);
        }
    }

    private void DrawText(string text, Vector2 position, Color color)
    {
        _spriteBatch.DrawString(_font, text, position, color);
    }

    private void DrawSunCenterButton(Viewport viewport)
    {
        var button = GetSunCenterButtonRectangle(viewport);
        var sunCenter = new Vector2(button.X + 25, button.Center.Y);
        var label = "Centralizar Sol";

        _primitives.FillRectangle(button, new Color(8, 14, 28, 230));
        _primitives.DrawRectangle(button, new Color(255, 206, 74, 230), 2);
        _primitives.DrawBody(sunCenter, 15f, 1f, new Color(255, 206, 74));
        _primitives.DrawCircleOutline(sunCenter, 18f, new Color(255, 184, 70, 170), 1f);
        DrawText(label, new Vector2(button.X + 48, button.Y + 15), new Color(240, 232, 198));
    }

    private void DrawFilterButton(SolarSystemState solarSystem, Viewport viewport)
    {
        var button = GetFilterButtonRectangle(viewport);
        var fillColor = solarSystem.IsFilterMenuOpen ? new Color(32, 91, 128, 230) : new Color(8, 14, 28, 230);
        var borderColor = solarSystem.ShowCenterOfMass ? new Color(117, 220, 255) : new Color(78, 105, 150, 230);
        var label = "Filtros";
        var labelSize = _font.MeasureString(label);
        var labelPosition = new Vector2(
            button.Center.X - labelSize.X / 2f,
            button.Center.Y - labelSize.Y / 2f);

        _primitives.FillRectangle(button, fillColor);
        _primitives.DrawRectangle(button, borderColor, 2);
        DrawText(label, labelPosition, Color.White);
    }

    private void DrawFilterMenu(SolarSystemState solarSystem, Viewport viewport)
    {
        var menu = GetFilterMenuRectangle(viewport);
        var checkbox = GetCenterOfMassCheckboxRectangle(viewport);

        _primitives.FillRectangle(menu, new Color(8, 14, 28, 238));
        _primitives.DrawRectangle(menu, new Color(78, 105, 150, 230), 2);
        DrawText("Filtros", new Vector2(menu.X + 14, menu.Y + 12), Color.White);

        _primitives.FillRectangle(checkbox, new Color(18, 34, 58, 235));
        _primitives.DrawRectangle(checkbox, new Color(117, 160, 215, 230), 1);

        if (solarSystem.ShowCenterOfMass)
        {
            _primitives.DrawLine(new Vector2(checkbox.X + 4, checkbox.Y + 9), new Vector2(checkbox.X + 8, checkbox.Y + 14), new Color(117, 220, 255), 2f);
            _primitives.DrawLine(new Vector2(checkbox.X + 8, checkbox.Y + 14), new Vector2(checkbox.X + 15, checkbox.Y + 4), new Color(117, 220, 255), 2f);
        }

        DrawText("Centro de massa", new Vector2(checkbox.Right + 10, checkbox.Y - 2), new Color(216, 226, 242));
    }

}

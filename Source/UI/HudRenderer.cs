using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Astronomia;

public sealed class HudRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly PrimitiveRenderer _primitives;
    private readonly TextureAssets _textures;
    private Language _currentLanguage = Language.Portuguese;

    public HudRenderer(SpriteBatch spriteBatch, SpriteFont font, PrimitiveRenderer primitives, TextureAssets textures)
    {
        _spriteBatch = spriteBatch;
        _font = font;
        _primitives = primitives;
        _textures = textures;
    }

    public Rectangle PanelRectangle => new(18, 18, 350, 124);

    public Rectangle GetLanguageButtonRectangle(Viewport viewport)
    {
        return new Rectangle(viewport.Width - 68, 18, 44, 44);
    }

    public Rectangle GetLanguageMenuRectangle(Viewport viewport)
    {
        var button = GetLanguageButtonRectangle(viewport);
        return new Rectangle(button.Right - 168, button.Bottom + 10, 168, 92);
    }

    public Rectangle GetEnglishButtonRectangle(Viewport viewport)
    {
        var menu = GetLanguageMenuRectangle(viewport);
        return new Rectangle(menu.X + 12, menu.Y + 12, menu.Width - 24, 30);
    }

    public Rectangle GetPortugueseButtonRectangle(Viewport viewport)
    {
        var menu = GetLanguageMenuRectangle(viewport);
        return new Rectangle(menu.X + 12, menu.Y + 50, menu.Width - 24, 30);
    }

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

    public void Draw(SolarSystemState solarSystem, float zoom, Viewport viewport, bool isLanguageMenuOpen, Language selectedLanguage)
    {
        _currentLanguage = selectedLanguage;

        var panel = PanelRectangle;
        _primitives.FillRectangle(panel, new Color(8, 14, 28, 215));
        _primitives.DrawRectangle(panel, new Color(78, 105, 150, 210), 1);

        var y = 32;
        DrawText(TextCatalog.Get(selectedLanguage, "hud.title"), new Vector2(34, y), Color.White);
        y += 30;
        DrawText($"{TextCatalog.Get(selectedLanguage, "hud.time")}: {TextCatalog.Get(selectedLanguage, "hud.day")} {solarSystem.SimulationDays:0.0}", new Vector2(34, y), new Color(210, 222, 240));
        y += 23;
        DrawText($"{TextCatalog.Get(selectedLanguage, "hud.scale")}: {solarSystem.DaysPerSecond:0.##} {TextCatalog.Get(selectedLanguage, "hud.daysPerSecond")}", new Vector2(34, y), new Color(210, 222, 240));
        y += 23;
        DrawText($"{TextCatalog.Get(selectedLanguage, "hud.zoom")}: {zoom:0.00}x", new Vector2(34, y), new Color(210, 222, 240));

        DrawLanguageButton(viewport, isLanguageMenuOpen);

        if (isLanguageMenuOpen)
            DrawLanguageMenu(viewport, selectedLanguage);

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

    private void DrawLanguageButton(Viewport viewport, bool isLanguageMenuOpen)
    {
        var button = GetLanguageButtonRectangle(viewport);
        var icon = new Rectangle(button.X + 7, button.Y + 7, 30, 30);

        _spriteBatch.Draw(_textures.LanguageIcon, icon, Color.White);
    }

    private void DrawLanguageMenu(Viewport viewport, Language selectedLanguage)
    {
        var menu = GetLanguageMenuRectangle(viewport);

        _primitives.FillRectangle(menu, new Color(8, 14, 28, 242));
        _primitives.DrawRectangle(menu, new Color(78, 105, 150, 230), 2);
        DrawLanguageOption(GetEnglishButtonRectangle(viewport), TextCatalog.Get(selectedLanguage, "language.english"), selectedLanguage == Language.English);
        DrawLanguageOption(GetPortugueseButtonRectangle(viewport), TextCatalog.Get(selectedLanguage, "language.portuguese"), selectedLanguage == Language.Portuguese);
    }

    private void DrawLanguageOption(Rectangle button, string label, bool selected)
    {
        var fillColor = selected ? new Color(32, 91, 128, 225) : new Color(18, 34, 58, 210);
        var borderColor = selected ? new Color(117, 220, 255, 230) : new Color(78, 105, 150, 160);
        var textColor = selected ? Color.White : new Color(216, 226, 242);
        var labelSize = _font.MeasureString(label);
        var labelPosition = new Vector2(button.X + 12, button.Center.Y - labelSize.Y / 2f);

        _primitives.FillRectangle(button, fillColor);
        _primitives.DrawRectangle(button, borderColor, 1);
        DrawText(label, labelPosition, textColor);
    }

    private void DrawSunCenterButton(Viewport viewport)
    {
        var button = GetSunCenterButtonRectangle(viewport);
        var sunCenter = new Vector2(button.X + 25, button.Center.Y);
        var label = TextCatalog.Get(_currentLanguage, "topDown.centerSun");

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
        var label = TextCatalog.Get(_currentLanguage, "filters.title");
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
        DrawText(TextCatalog.Get(_currentLanguage, "filters.title"), new Vector2(menu.X + 14, menu.Y + 12), Color.White);

        _primitives.FillRectangle(checkbox, new Color(18, 34, 58, 235));
        _primitives.DrawRectangle(checkbox, new Color(117, 160, 215, 230), 1);

        if (solarSystem.ShowCenterOfMass)
        {
            _primitives.DrawLine(new Vector2(checkbox.X + 4, checkbox.Y + 9), new Vector2(checkbox.X + 8, checkbox.Y + 14), new Color(117, 220, 255), 2f);
            _primitives.DrawLine(new Vector2(checkbox.X + 8, checkbox.Y + 14), new Vector2(checkbox.X + 15, checkbox.Y + 4), new Color(117, 220, 255), 2f);
        }

        DrawText(TextCatalog.Get(_currentLanguage, "filters.centerOfMass"), new Vector2(checkbox.Right + 10, checkbox.Y - 2), new Color(216, 226, 242));
    }

}

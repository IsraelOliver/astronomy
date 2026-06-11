using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace Astronomia;

public sealed class InclinedSimulationState : IGameState
{
    private const int ScreenshotWidth = 3840;
    private const int ScreenshotHeight = 2160;
    private const int ScreenshotMultiSampleCount = 16;

    private readonly Game _game;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly CameraController _camera = new();
    private readonly SolarSystemState _solarSystem;
    private readonly SpriteBatch _spriteBatch;
    private readonly HudRenderer _hudRenderer;
    private readonly StudyPanelRenderer _studyPanelRenderer;
    private readonly SolarSystemRenderer _solarSystemRenderer;
    private readonly UserSettings _settings;

    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private bool _screenshotRequested;
    private bool _isLanguageMenuOpen;
    private Language _selectedLanguage = Language.English;

    public InclinedSimulationState(
        Game game,
        GraphicsDevice graphicsDevice,
        SolarSystemState solarSystem,
        SpriteBatch spriteBatch,
        HudRenderer hudRenderer,
        StudyPanelRenderer studyPanelRenderer,
        SolarSystemRenderer solarSystemRenderer,
        UserSettings settings)
    {
        _game = game;
        _graphicsDevice = graphicsDevice;
        _solarSystem = solarSystem;
        _spriteBatch = spriteBatch;
        _hudRenderer = hudRenderer;
        _studyPanelRenderer = studyPanelRenderer;
        _solarSystemRenderer = solarSystemRenderer;
        _settings = settings;
        _selectedLanguage = _settings.Language;

        _camera.Reset(_solarSystem.ViewMode);
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            _game.Exit();

        HandleKeyboardShortcuts(keyboard);
        HandleMouseInput(mouse);

        if (!_solarSystem.HasSelectedBody)
        {
            var wheelDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
            _camera.UpdateFreeCamera(keyboard, mouse, _previousMouse, wheelDelta, elapsedSeconds, _solarSystem.ViewMode);
        }

        _solarSystem.Advance(elapsedSeconds);
        _camera.UpdateFocus(_solarSystem, _graphicsDevice.Viewport, elapsedSeconds);

        _previousKeyboard = keyboard;
        _previousMouse = mouse;
    }

    public void Draw(GameTime gameTime)
    {
        DrawFrame(_graphicsDevice.Viewport, _camera.GetSystemCenter(_graphicsDevice.Viewport), _camera.Zoom, new Vector2(_previousMouse.X, _previousMouse.Y));

        if (_screenshotRequested)
        {
            SaveScreenshot();
            _screenshotRequested = false;
        }
    }

    private void SaveScreenshot()
    {
        var previousViewport = _graphicsDevice.Viewport;
        var screenshotViewport = new Viewport(0, 0, ScreenshotWidth, ScreenshotHeight);
        var renderScale = MathF.Min(
            ScreenshotWidth / (float)previousViewport.Width,
            ScreenshotHeight / (float)previousViewport.Height);
        var screenshotCenter = new Vector2(ScreenshotWidth / 2f, ScreenshotHeight / 2f) + _camera.Offset * renderScale;
        var screenshotMouse = ScalePointToScreenshot(new Vector2(_previousMouse.X, _previousMouse.Y), previousViewport, screenshotViewport, renderScale);

        using var screenshot = new RenderTarget2D(
            _graphicsDevice,
            ScreenshotWidth,
            ScreenshotHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            ScreenshotMultiSampleCount,
            RenderTargetUsage.PreserveContents);

        _graphicsDevice.SetRenderTarget(screenshot);
        _graphicsDevice.Viewport = screenshotViewport;
        DrawFrame(screenshotViewport, screenshotCenter, _camera.Zoom * renderScale, screenshotMouse);
        _graphicsDevice.SetRenderTarget(null);
        _graphicsDevice.Viewport = previousViewport;

        var imageDirectory = Path.Combine(GetProjectRootDirectory(), "image");
        Directory.CreateDirectory(imageDirectory);

        var fileName = $"screenshot_4k_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        var path = Path.Combine(imageDirectory, fileName);

        using var stream = File.Create(path);
        screenshot.SaveAsPng(stream, ScreenshotWidth, ScreenshotHeight);
    }

    private void DrawFrame(Viewport viewport, Vector2 center, float zoom, Vector2 mousePosition)
    {
        _graphicsDevice.Clear(SolarSystemRenderer.BackgroundColor);

        _solarSystemRenderer.Draw(_solarSystem, center, zoom, viewport, _selectedLanguage);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _hudRenderer.Draw(_solarSystem, zoom, viewport, _isLanguageMenuOpen, _selectedLanguage);
        _studyPanelRenderer.Draw(_solarSystem, viewport, _selectedLanguage);
        _studyPanelRenderer.DrawHoverTooltip(
            _solarSystem,
            viewport,
            mousePosition,
            center,
            zoom,
            _hudRenderer.PanelRectangle,
            _hudRenderer.GetLanguageButtonRectangle(viewport),
            _hudRenderer.GetLanguageMenuRectangle(viewport),
            _isLanguageMenuOpen,
            _selectedLanguage);

        _spriteBatch.End();
    }

    private static Vector2 ScalePointToScreenshot(Vector2 point, Viewport sourceViewport, Viewport targetViewport, float scale)
    {
        var sourceCenter = new Vector2(sourceViewport.Width / 2f, sourceViewport.Height / 2f);
        var targetCenter = new Vector2(targetViewport.Width / 2f, targetViewport.Height / 2f);
        return targetCenter + (point - sourceCenter) * scale;
    }

    private static string GetProjectRootDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Astronomia.csproj")))
            directory = directory.Parent;

        return directory?.FullName ?? AppContext.BaseDirectory;
    }

    private void HandleKeyboardShortcuts(KeyboardState keyboard)
    {
        if (WasPressed(keyboard, Keys.Space))
            _solarSystem.TogglePaused();

        if (WasPressed(keyboard, Keys.R))
        {
            _solarSystem.Reset();
            _camera.Reset(_solarSystem.ViewMode);
        }

        if (WasPressed(keyboard, Keys.C))
            _solarSystem.ClearSelection();

        if (WasPressed(keyboard, Keys.F2))
            _screenshotRequested = true;

        if (WasPressed(keyboard, Keys.OemPlus) || WasPressed(keyboard, Keys.Add))
            _solarSystem.IncreaseTimeScale();

        if (WasPressed(keyboard, Keys.OemMinus) || WasPressed(keyboard, Keys.Subtract))
            _solarSystem.DecreaseTimeScale();
    }

    private void HandleMouseInput(MouseState mouse)
    {
        var mousePosition = new Vector2(mouse.X, mouse.Y);

        if (mouse.LeftButton == ButtonState.Pressed &&
            _studyPanelRenderer.TryGetEditorSliderAt(_solarSystem, _graphicsDevice.Viewport, mousePosition, out var field, out var normalizedValue))
        {
            _solarSystem.ApplyEditorSlider(field, normalizedValue);
            return;
        }

        if (!WasLeftClicked(mouse))
            return;

        var sunButton = _hudRenderer.GetSunCenterButtonRectangle(_graphicsDevice.Viewport);
        var filterButton = _hudRenderer.GetFilterButtonRectangle(_graphicsDevice.Viewport);
        var filterMenu = _hudRenderer.GetFilterMenuRectangle(_graphicsDevice.Viewport);
        var centerOfMassCheckbox = _hudRenderer.GetCenterOfMassCheckboxRectangle(_graphicsDevice.Viewport);
        var languageButton = _hudRenderer.GetLanguageButtonRectangle(_graphicsDevice.Viewport);
        var languageMenu = _hudRenderer.GetLanguageMenuRectangle(_graphicsDevice.Viewport);
        var englishButton = _hudRenderer.GetEnglishButtonRectangle(_graphicsDevice.Viewport);
        var portugueseButton = _hudRenderer.GetPortugueseButtonRectangle(_graphicsDevice.Viewport);

        if (Contains(languageButton, mousePosition))
        {
            _isLanguageMenuOpen = !_isLanguageMenuOpen;
            return;
        }

        if (_isLanguageMenuOpen && Contains(englishButton, mousePosition))
        {
            SelectLanguage(Language.English);
            return;
        }

        if (_isLanguageMenuOpen && Contains(portugueseButton, mousePosition))
        {
            SelectLanguage(Language.Portuguese);
            return;
        }

        if (_isLanguageMenuOpen && Contains(languageMenu, mousePosition))
            return;

        _isLanguageMenuOpen = false;

        if (_solarSystem.ViewMode == SystemViewMode.TopDown && Contains(sunButton, mousePosition))
        {
            _solarSystem.ClearSelection();
            _camera.CenterOnSun(_solarSystem);
            return;
        }

        if (_solarSystem.ViewMode == SystemViewMode.TopDown && Contains(filterButton, mousePosition))
        {
            _solarSystem.ToggleFilterMenu();
            return;
        }

        if (_solarSystem.IsFilterMenuOpen && Contains(centerOfMassCheckbox, mousePosition))
        {
            _solarSystem.ToggleCenterOfMassFilter();
            return;
        }

        if (_solarSystem.IsFilterMenuOpen && Contains(filterMenu, mousePosition))
            return;

        SelectionService.SelectBodyAt(
            mousePosition,
            _solarSystem,
            _camera.GetSystemCenter(_graphicsDevice.Viewport),
            _camera.Zoom,
            _hudRenderer.PanelRectangle,
            _studyPanelRenderer.GetPanelRectangle(_solarSystem, _graphicsDevice.Viewport, _selectedLanguage),
            languageButton,
            languageMenu,
            _isLanguageMenuOpen);
    }

    private bool WasPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }

    private void SelectLanguage(Language language)
    {
        _selectedLanguage = language;
        _settings.Language = language;
        _settings.Save();
        _isLanguageMenuOpen = false;
    }

    private bool WasLeftClicked(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
    }

    private static bool Contains(Rectangle rectangle, Vector2 point)
    {
        return point.X >= rectangle.Left &&
            point.X <= rectangle.Right &&
            point.Y >= rectangle.Top &&
            point.Y <= rectangle.Bottom;
    }
}

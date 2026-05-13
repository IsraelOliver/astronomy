using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace Astronomia;

public class Game1 : Game
{
    private const int ScreenshotWidth = 3840;
    private const int ScreenshotHeight = 2160;
    private const int ScreenshotMultiSampleCount = 16;

    private readonly GraphicsDeviceManager _graphics;
    private readonly CameraController _camera = new();

    private SolarSystemState _solarSystem = null!;
    private SpriteBatch _spriteBatch = null!;
    private HudRenderer _hudRenderer = null!;
    private StudyPanelRenderer _studyPanelRenderer = null!;
    private SolarSystemRenderer _solarSystemRenderer = null!;

    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private bool _screenshotRequested;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        _graphics.PreferMultiSampling = true;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.PreparingDeviceSettings += (_, args) =>
        {
            args.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 16;
        };
        Window.Title = "Astronomia - Simulador Orbital";

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _solarSystem = SolarSystemFactory.Create(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var font = Content.Load<SpriteFont>("UiFont");
        var textures = new TextureAssets(
            TextureFactory.CreatePixel(GraphicsDevice),
            TextureFactory.CreateCircle(GraphicsDevice, 256),
            TextureFactory.CreateLine(GraphicsDevice));
        var shaders = new ShaderAssets(
            Content.Load<Effect>("Effects/PassThrough"),
            Content.Load<Effect>("Effects/SpaceBackground"),
            Content.Load<Effect>("Effects/SoftCircleMask"),
            Content.Load<Effect>("Effects/SunGlow"),
            Content.Load<Effect>("Effects/SolarDust"),
            Content.Load<Effect>("Effects/ToonPlanet"),
            Content.Load<Effect>("Effects/SaturnRings"));

        var primitives = new PrimitiveRenderer(_spriteBatch, textures);
        _solarSystemRenderer = new SolarSystemRenderer(_spriteBatch, textures, shaders, primitives, font);
        _hudRenderer = new HudRenderer(_spriteBatch, font, primitives);
        _studyPanelRenderer = new StudyPanelRenderer(_spriteBatch, font, primitives);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        HandleKeyboardShortcuts(keyboard);
        HandleMouseInput(mouse);

        if (!_solarSystem.HasSelectedBody)
        {
            var wheelDelta = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
            _camera.UpdateFreeCamera(keyboard, mouse, _previousMouse, wheelDelta, elapsedSeconds, _solarSystem.ViewMode);
        }

        _solarSystem.Advance(elapsedSeconds);
        _camera.UpdateFocus(_solarSystem, GraphicsDevice.Viewport, elapsedSeconds);

        _previousKeyboard = keyboard;
        _previousMouse = mouse;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        DrawFrame(GraphicsDevice.Viewport, _camera.GetSystemCenter(GraphicsDevice.Viewport), _camera.Zoom, new Vector2(_previousMouse.X, _previousMouse.Y));

        if (_screenshotRequested)
        {
            SaveScreenshot();
            _screenshotRequested = false;
        }

        base.Draw(gameTime);
    }

    private void SaveScreenshot()
    {
        var previousViewport = GraphicsDevice.Viewport;
        var screenshotViewport = new Viewport(0, 0, ScreenshotWidth, ScreenshotHeight);
        var renderScale = MathF.Min(
            ScreenshotWidth / (float)previousViewport.Width,
            ScreenshotHeight / (float)previousViewport.Height);
        var screenshotCenter = new Vector2(ScreenshotWidth / 2f, ScreenshotHeight / 2f) + _camera.Offset * renderScale;
        var screenshotMouse = ScalePointToScreenshot(new Vector2(_previousMouse.X, _previousMouse.Y), previousViewport, screenshotViewport, renderScale);

        using var screenshot = new RenderTarget2D(
            GraphicsDevice,
            ScreenshotWidth,
            ScreenshotHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            ScreenshotMultiSampleCount,
            RenderTargetUsage.PreserveContents);

        GraphicsDevice.SetRenderTarget(screenshot);
        GraphicsDevice.Viewport = screenshotViewport;
        DrawFrame(screenshotViewport, screenshotCenter, _camera.Zoom * renderScale, screenshotMouse);
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Viewport = previousViewport;

        var imageDirectory = Path.Combine(GetProjectRootDirectory(), "image");
        Directory.CreateDirectory(imageDirectory);

        var fileName = $"screenshot_4k_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        var path = Path.Combine(imageDirectory, fileName);

        using var stream = File.Create(path);
        screenshot.SaveAsPng(stream, ScreenshotWidth, ScreenshotHeight);
    }

    private void DrawFrame(Viewport viewport, Vector2 center, float zoom, Vector2 mousePosition)
    {
        GraphicsDevice.Clear(SolarSystemRenderer.BackgroundColor);

        _solarSystemRenderer.Draw(_solarSystem, center, zoom, viewport);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _hudRenderer.Draw(_solarSystem, zoom, viewport);
        _studyPanelRenderer.Draw(_solarSystem, viewport);
        _studyPanelRenderer.DrawHoverTooltip(
            _solarSystem,
            viewport,
            mousePosition,
            center,
            zoom,
            _hudRenderer.PanelRectangle,
            _hudRenderer.GetViewModeButtonRectangle(viewport));

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
            _studyPanelRenderer.TryGetEditorSliderAt(_solarSystem, GraphicsDevice.Viewport, mousePosition, out var field, out var normalizedValue))
        {
            _solarSystem.ApplyEditorSlider(field, normalizedValue);
            return;
        }

        if (!WasLeftClicked(mouse))
            return;

        var viewModeButton = _hudRenderer.GetViewModeButtonRectangle(GraphicsDevice.Viewport);
        var sunButton = _hudRenderer.GetSunCenterButtonRectangle(GraphicsDevice.Viewport);
        var filterButton = _hudRenderer.GetFilterButtonRectangle(GraphicsDevice.Viewport);
        var filterMenu = _hudRenderer.GetFilterMenuRectangle(GraphicsDevice.Viewport);
        var centerOfMassCheckbox = _hudRenderer.GetCenterOfMassCheckboxRectangle(GraphicsDevice.Viewport);

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

        if (Contains(viewModeButton, mousePosition))
        {
            _solarSystem.ToggleViewMode();
            _camera.Reset(_solarSystem.ViewMode);
            return;
        }

        SelectionService.SelectBodyAt(
            mousePosition,
            _solarSystem,
            _camera.GetSystemCenter(GraphicsDevice.Viewport),
            _camera.Zoom,
            _hudRenderer.PanelRectangle,
            _studyPanelRenderer.GetPanelRectangle(_solarSystem, GraphicsDevice.Viewport),
            viewModeButton);
    }

    private bool WasPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
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

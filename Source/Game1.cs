using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Astronomia;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private IGameState? _activeState;

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

    protected override void LoadContent()
    {
        var spriteBatch = new SpriteBatch(GraphicsDevice);

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

        var primitives = new PrimitiveRenderer(spriteBatch, textures);
        var solarSystem = SolarSystemFactory.Create(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        var solarSystemRenderer = new SolarSystemRenderer(spriteBatch, textures, shaders, primitives, font);
        var hudRenderer = new HudRenderer(spriteBatch, font, primitives);
        var studyPanelRenderer = new StudyPanelRenderer(spriteBatch, font, primitives);

        _activeState = new InclinedSimulationState(
            this,
            GraphicsDevice,
            solarSystem,
            spriteBatch,
            hudRenderer,
            studyPanelRenderer,
            solarSystemRenderer);
    }

    protected override void Update(GameTime gameTime)
    {
        _activeState?.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _activeState?.Draw(gameTime);
        base.Draw(gameTime);
    }
}

using Microsoft.Xna.Framework.Graphics;

namespace Astronomia;

public sealed record ShaderAssets(
    Effect PassThrough,
    Effect SpaceBackground,
    Effect SoftCircleMask,
    Effect SunGlow,
    Effect SolarDust,
    Effect ToonPlanet,
    Effect SaturnRings);

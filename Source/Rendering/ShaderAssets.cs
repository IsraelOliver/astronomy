using Microsoft.Xna.Framework.Graphics;

namespace Astronomia;

public sealed record ShaderAssets(
    Effect PassThrough,
    Effect SoftCircleMask,
    Effect SunGlow,
    Effect ToonPlanet,
    Effect SaturnRings);

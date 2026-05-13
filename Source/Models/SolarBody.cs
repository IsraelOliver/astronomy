using Microsoft.Xna.Framework;

namespace Astronomia;

public sealed record SolarBody(
    string Name,
    float VisualRadius,
    Color Color,
    string Type,
    int DiameterKm,
    float MassEarths,
    float GravityMs2,
    int SurfaceTemperatureC,
    string Summary);

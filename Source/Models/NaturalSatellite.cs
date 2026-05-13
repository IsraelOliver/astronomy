using Microsoft.Xna.Framework;

namespace Astronomia;

public sealed record NaturalSatellite(
    string Name,
    string ParentName,
    float Radius,
    Color Color,
    double MassKg,
    double AverageDistanceMeters,
    double OrbitalSpeedMetersPerSecond,
    float OrbitalPeriodDays);

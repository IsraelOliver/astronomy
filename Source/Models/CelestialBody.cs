using Microsoft.Xna.Framework;

namespace Astronomia;

public sealed record CelestialBody(
    string Name,
    float OrbitRadius,
    float OrbitalPeriodDays,
    float Radius,
    Color Color,
    float Phase,
    float DistanceAu,
    float Eccentricity,
    int DiameterKm,
    double MassKg,
    float MassEarths,
    float GravityMs2,
    int AverageTemperatureC,
    float RotationSpeedKmh,
    float OrbitalSpeedKms,
    string Summary,
    float OrbitArgumentDegrees = 0f,
    float OrbitPlaneTiltDegrees = 0f,
    bool HasRings = false,
    string RingStyle = "");

namespace Astronomia;

public sealed record PlanetDiagnostics(
    double CurrentSpeedKms,
    double AccelerationMs2,
    double GravitationalForceN,
    double DistanceFromCenterOfMassMeters,
    double SimplifiedOrbitalEnergyJ);

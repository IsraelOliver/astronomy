namespace Astronomia;

public static class PhysicsConstants
{
    public const double GravitationalConstant = 6.67430e-11;
    public const double AstronomicalUnitMeters = 149_597_870_700d;
    public const double EarthMassKg = 5.9724e24;
    public const double SecondsPerDay = 86_400d;
    public const double PixelsPerAstronomicalUnit = 104d;
    public const double PixelsPerMeter = PixelsPerAstronomicalUnit / AstronomicalUnitMeters;
}

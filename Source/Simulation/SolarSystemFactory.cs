using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Astronomia;

public static class SolarSystemFactory
{
    public static SolarSystemState Create(int starFieldWidth, int starFieldHeight)
    {
        var sun = CreateSun();
        var planets = CreatePlanets();
        var satellites = CreateSatellites();
        var stars = CreateStars(starFieldWidth, starFieldHeight);
        var gravitySimulation = new GravitySimulation(CreatePhysicalBodies(sun, planets, satellites));

        return new SolarSystemState(sun, planets, satellites, stars, gravitySimulation);
    }

    private static SolarBody CreateSun()
    {
        return new SolarBody(
            "Sol",
            19f,
            new Color(255, 206, 74),
            "Estrela ana amarela",
            1392700,
            333000f,
            274f,
            5500,
            "Fonte de luz e energia do Sistema Solar; concentra quase toda a massa do sistema.");
    }

    private static IReadOnlyList<CelestialBody> CreatePlanets()
    {
        return new[]
        {
            new CelestialBody("Mercurio", 46f, 88f, 3.2f, new Color(184, 174, 152), 0.9f, 0.39f, 0.2056f, 4879, 3.3011e23, 0.055f, 3.7f, 167, 10.9f, 47.36f, "Menor planeta e o mais proximo do Sol."),
            new CelestialBody("Venus", 72f, 225f, 5.1f, new Color(224, 175, 98), 2.1f, 0.72f, 0.0068f, 12104, 4.8675e24, 0.815f, 8.87f, 464, 6.5f, 35.02f, "Atmosfera muito densa e efeito estufa extremo."),
            new CelestialBody("Terra", 104f, 365f, 5.8f, new Color(75, 150, 232), 3.3f, 1.00f, 0.0167f, 12742, 5.9724e24, 1.00f, 9.81f, 15, 1674.4f, 29.78f, "Nosso planeta, com agua liquida estavel na superficie."),
            new CelestialBody("Marte", 142f, 687f, 4.5f, new Color(219, 98, 74), 4.4f, 1.52f, 0.0934f, 6779, 6.4171e23, 0.107f, 3.71f, -63, 866f, 24.07f, "Planeta rochoso frio, com calotas polares e poeira rica em ferro."),
            new CelestialBody("Jupiter", 220f, 4333f, 11.4f, new Color(216, 179, 130), 5.5f, 5.20f, 0.0489f, 139820, 1.8982e27, 317.8f, 24.79f, -110, 45300f, 13.07f, "Maior planeta; sua gravidade influencia muitos corpos menores."),
            new CelestialBody("Saturno", 295f, 10759f, 10.4f, new Color(222, 199, 145), 1.4f, 9.58f, 0.0565f, 116460, 5.6834e26, 95.2f, 10.44f, -140, 35500f, 9.68f, "Gigante gasoso famoso por seus aneis extensos.", HasRings: true, RingStyle: "Saturn"),
            new CelestialBody("Urano", 365f, 30687f, 8.1f, new Color(139, 217, 220), 2.7f, 19.20f, 0.0463f, 50724, 8.6810e25, 14.5f, 8.69f, -195, 9320f, 6.80f, "Gigante gelado com eixo de rotacao muito inclinado e aneis estreitos, escuros e dificeis de observar.", HasRings: true, RingStyle: "Uranus"),
            new CelestialBody("Netuno", 430f, 60190f, 7.9f, new Color(76, 116, 220), 3.8f, 30.05f, 0.0090f, 49244, 1.0241e26, 17.1f, 11.15f, -200, 9660f, 5.43f, "Gigante gelado distante, com ventos muito intensos."),
            new CelestialBody("Plutao", 565f, 90560f, 2.7f, new Color(190, 158, 132), 5.9f, 39.48f, 0.2488f, 2376, 1.303e22, 0.00218f, 0.62f, -229, 48.7f, 4.74f, "Planeta anao distante, com orbita muito eliptica e inclinada em relacao aos planetas principais.", OrbitArgumentDegrees: 108f, OrbitPlaneTiltDegrees: 18f)
        };
    }

    private static IReadOnlyList<Star> CreateStars(int width, int height)
    {
        var stars = new List<Star>();
        var random = new Random(42);

        for (var i = 0; i < 240; i++)
        {
            stars.Add(new Star(
                new Vector2(random.Next(0, width), random.Next(0, height)),
                (float)(0.35 + random.NextDouble() * 1.6),
                (float)(0.25 + random.NextDouble() * 0.55)));
        }

        return stars;
    }

    private static IReadOnlyList<NaturalSatellite> CreateSatellites()
    {
        return new[]
        {
            new NaturalSatellite(
                "Lua",
                "Terra",
                2.1f,
                new Color(218, 218, 205),
                7.342e22,
                384_400_000d,
                1_022d,
                27.32f)
        };
    }

    private static IReadOnlyList<PhysicalBody> CreatePhysicalBodies(SolarBody sun, IReadOnlyList<CelestialBody> planets, IReadOnlyList<NaturalSatellite> satellites)
    {
        var bodies = new List<PhysicalBody>();
        var planetBodies = new Dictionary<string, PhysicalBody>();
        var sunMassKg = sun.MassEarths * PhysicsConstants.EarthMassKg;
        var totalPlanetMomentum = PhysicsVector2.Zero;

        foreach (var planet in planets)
        {
            var angle = planet.Phase + MathHelper.ToRadians(planet.OrbitArgumentDegrees);
            var semiMajorAxisMeters = planet.DistanceAu * PhysicsConstants.AstronomicalUnitMeters;
            var perihelionDistanceMeters = semiMajorAxisMeters * (1d - planet.Eccentricity);
            var perihelionSpeedMetersPerSecond = Math.Sqrt(
                PhysicsConstants.GravitationalConstant *
                (sunMassKg + planet.MassKg) *
                (1d + planet.Eccentricity) /
                (semiMajorAxisMeters * (1d - planet.Eccentricity)));
            var position = new PhysicsVector2(
                Math.Cos(angle) * perihelionDistanceMeters,
                Math.Sin(angle) * perihelionDistanceMeters);
            var velocity = new PhysicsVector2(
                -Math.Sin(angle) * perihelionSpeedMetersPerSecond,
                Math.Cos(angle) * perihelionSpeedMetersPerSecond);

            var planetBody = new PhysicalBody(planet.Name, planet.MassKg, position, velocity);
            bodies.Add(planetBody);
            planetBodies[planet.Name] = planetBody;
            totalPlanetMomentum += velocity * planet.MassKg;
        }

        foreach (var satellite in satellites)
        {
            if (!planetBodies.TryGetValue(satellite.ParentName, out var parentBody))
                continue;

            var satellitePhase = 0.65d;
            var offset = new PhysicsVector2(
                Math.Cos(satellitePhase) * satellite.AverageDistanceMeters,
                Math.Sin(satellitePhase) * satellite.AverageDistanceMeters);
            var relativeVelocity = new PhysicsVector2(
                -Math.Sin(satellitePhase) * satellite.OrbitalSpeedMetersPerSecond,
                Math.Cos(satellitePhase) * satellite.OrbitalSpeedMetersPerSecond);
            var satelliteBody = new PhysicalBody(
                satellite.Name,
                satellite.MassKg,
                parentBody.PositionMeters + offset,
                parentBody.VelocityMetersPerSecond + relativeVelocity);

            bodies.Add(satelliteBody);
            totalPlanetMomentum += satelliteBody.VelocityMetersPerSecond * satellite.MassKg;
        }

        var sunVelocity = totalPlanetMomentum / -sunMassKg;
        bodies.Insert(0, new PhysicalBody(sun.Name, sunMassKg, PhysicsVector2.Zero, sunVelocity));

        return bodies;
    }
}

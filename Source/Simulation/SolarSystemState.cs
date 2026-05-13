using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Astronomia;

public sealed class SolarSystemState
{
    private const int MaximumTrailPoints = 900;
    private const double MinimumMassFactor = 1e-4;
    private const double MaximumMassFactor = 1e8;
    private const double MaximumRotationFactor = 10d;
    private const double MaximumTranslationFactor = 5d;

    private readonly Dictionary<string, double> _rotationSpeedsKmh = new();
    private readonly Dictionary<string, Queue<PhysicsVector2>> _planetTrails = new();

    public SolarSystemState(
        SolarBody sun,
        IReadOnlyList<CelestialBody> planets,
        IReadOnlyList<NaturalSatellite> satellites,
        IReadOnlyList<Star> stars,
        GravitySimulation gravitySimulation)
    {
        Sun = sun;
        Planets = planets;
        Satellites = satellites;
        Stars = stars;
        GravitySimulation = gravitySimulation;

        ResetRotationSpeeds();
        ResetTrails();
    }

    public SolarBody Sun { get; }
    public IReadOnlyList<CelestialBody> Planets { get; }
    public IReadOnlyList<NaturalSatellite> Satellites { get; }
    public IReadOnlyList<Star> Stars { get; }
    public GravitySimulation GravitySimulation { get; }

    public float SimulationDays { get; private set; }
    public float DaysPerSecond { get; private set; } = 14f;
    public bool Paused { get; private set; }
    public SystemViewMode ViewMode { get; private set; } = SystemViewMode.TopDown;
    public CelestialBody? SelectedPlanet { get; private set; }
    public bool IsSunSelected { get; private set; }
    public bool IsFilterMenuOpen { get; private set; }
    public bool ShowCenterOfMass { get; private set; }

    public bool HasSelectedBody => SelectedPlanet is not null || IsSunSelected;

    public void Advance(float elapsedSeconds)
    {
        if (Paused)
            return;

        SimulationDays += elapsedSeconds * DaysPerSecond;

        GravitySimulation.Step(elapsedSeconds * DaysPerSecond * PhysicsConstants.SecondsPerDay);
        RecordTrailPoints();
    }

    public void TogglePaused()
    {
        Paused = !Paused;
    }

    public void IncreaseTimeScale()
    {
        DaysPerSecond = MathHelper.Clamp(DaysPerSecond * 1.4f, 0.25f, 600f);
    }

    public void DecreaseTimeScale()
    {
        DaysPerSecond = MathHelper.Clamp(DaysPerSecond / 1.4f, 0.25f, 600f);
    }

    public void SelectPlanet(CelestialBody planet)
    {
        SelectedPlanet = planet;
        IsSunSelected = false;
    }

    public void SelectSun()
    {
        SelectedPlanet = null;
        IsSunSelected = true;
    }

    public void ClearSelection()
    {
        SelectedPlanet = null;
        IsSunSelected = false;
    }

    public void ToggleFilterMenu()
    {
        IsFilterMenuOpen = !IsFilterMenuOpen;
    }

    public void ToggleCenterOfMassFilter()
    {
        ShowCenterOfMass = !ShowCenterOfMass;
    }

    public double GetPlanetMassKg(CelestialBody planet)
    {
        return GravitySimulation.GetBody(planet.Name)?.MassKg ?? planet.MassKg;
    }

    public double GetPlanetRotationSpeedKmh(CelestialBody planet)
    {
        return _rotationSpeedsKmh.TryGetValue(planet.Name, out var speed)
            ? speed
            : planet.RotationSpeedKmh;
    }

    public double GetPlanetTranslationSpeedKms(CelestialBody planet)
    {
        var body = GravitySimulation.GetBody(planet.Name);
        return body is null
            ? planet.OrbitalSpeedKms
            : body.VelocityMetersPerSecond.Length / 1_000d;
    }

    public IReadOnlyCollection<PhysicsVector2> GetPlanetTrail(CelestialBody planet)
    {
        return _planetTrails.TryGetValue(planet.Name, out var trail)
            ? trail
            : System.Array.Empty<PhysicsVector2>();
    }

    public IReadOnlyCollection<PhysicsVector2> GetSatelliteTrail(NaturalSatellite satellite)
    {
        return _planetTrails.TryGetValue(satellite.Name, out var trail)
            ? trail
            : System.Array.Empty<PhysicsVector2>();
    }

    public void ApplyEditorSlider(PlanetEditorField field, double normalizedValue)
    {
        if (SelectedPlanet is null)
            return;

        normalizedValue = MathHelper.Clamp((float)normalizedValue, 0f, 1f);

        switch (field)
        {
            case PlanetEditorField.Mass:
                SetSelectedPlanetMass(GetMassFromNormalizedValue(SelectedPlanet, normalizedValue));
                break;
            case PlanetEditorField.Rotation:
                SetSelectedPlanetRotation(SelectedPlanet.RotationSpeedKmh * MaximumRotationFactor * normalizedValue);
                break;
            case PlanetEditorField.Translation:
                SetSelectedPlanetVelocity(SelectedPlanet.OrbitalSpeedKms * MaximumTranslationFactor * normalizedValue);
                break;
        }
    }

    public double GetEditorNormalizedValue(CelestialBody planet, PlanetEditorField field)
    {
        return field switch
        {
            PlanetEditorField.Mass => GetNormalizedMassValue(planet, GetPlanetMassKg(planet)),
            PlanetEditorField.Rotation => MathHelper.Clamp((float)(GetPlanetRotationSpeedKmh(planet) / (planet.RotationSpeedKmh * MaximumRotationFactor)), 0f, 1f),
            PlanetEditorField.Translation => MathHelper.Clamp((float)(GetPlanetTranslationSpeedKms(planet) / (planet.OrbitalSpeedKms * MaximumTranslationFactor)), 0f, 1f),
            _ => 0d,
        };
    }

    public PlanetDiagnostics? GetPlanetDiagnostics(CelestialBody planet)
    {
        var body = GravitySimulation.GetBody(planet.Name);
        var sunBody = GravitySimulation.GetBody(Sun.Name);

        if (body is null || sunBody is null)
            return null;

        var acceleration = GravitySimulation.GetAccelerationForBody(planet.Name);
        var centerOfMass = GravitySimulation.GetCenterOfMass();
        var distanceFromCenterOfMass = (body.PositionMeters - centerOfMass).Length;
        var relativeVelocity = body.VelocityMetersPerSecond - sunBody.VelocityMetersPerSecond;
        var distanceFromSun = (body.PositionMeters - sunBody.PositionMeters).Length;
        var kineticEnergy = 0.5d * body.MassKg * relativeVelocity.LengthSquared;
        var potentialEnergy = distanceFromSun > 0d
            ? -PhysicsConstants.GravitationalConstant * sunBody.MassKg * body.MassKg / distanceFromSun
            : 0d;

        return new PlanetDiagnostics(
            body.VelocityMetersPerSecond.Length / 1_000d,
            acceleration.Length,
            body.MassKg * acceleration.Length,
            distanceFromCenterOfMass,
            kineticEnergy + potentialEnergy);
    }

    public void ToggleViewMode()
    {
        ViewMode = ViewMode == SystemViewMode.Inclined
            ? SystemViewMode.TopDown
            : SystemViewMode.Inclined;

        ClearSelection();
    }

    public void Reset()
    {
        SimulationDays = 0f;
        DaysPerSecond = 14f;
        ViewMode = SystemViewMode.TopDown;
        GravitySimulation.Reset();
        ResetRotationSpeeds();
        ResetTrails();
        IsFilterMenuOpen = false;
        ShowCenterOfMass = false;
        ClearSelection();
    }

    private void SetSelectedPlanetMass(double massKg)
    {
        var body = GravitySimulation.GetBody(SelectedPlanet!.Name);
        if (body is not null)
            body.MassKg = massKg;
    }

    private void SetSelectedPlanetRotation(double rotationSpeedKmh)
    {
        _rotationSpeedsKmh[SelectedPlanet!.Name] = rotationSpeedKmh;
    }

    private void SetSelectedPlanetVelocity(double speedKms)
    {
        var body = GravitySimulation.GetBody(SelectedPlanet!.Name);
        if (body is null)
            return;

        var speedMetersPerSecond = speedKms * 1_000d;
        var currentSpeed = body.VelocityMetersPerSecond.Length;
        var direction = currentSpeed > 0.0001d
            ? body.VelocityMetersPerSecond / currentSpeed
            : GetFallbackTangentialDirection(body);

        body.VelocityMetersPerSecond = direction * speedMetersPerSecond;
    }

    private void ResetRotationSpeeds()
    {
        _rotationSpeedsKmh.Clear();

        foreach (var planet in Planets)
            _rotationSpeedsKmh[planet.Name] = planet.RotationSpeedKmh;
    }

    private static double GetMassFromNormalizedValue(CelestialBody planet, double normalizedValue)
    {
        var minMass = planet.MassKg * MinimumMassFactor;
        var maxMass = planet.MassKg * MaximumMassFactor;
        var logMin = System.Math.Log10(minMass);
        var logMax = System.Math.Log10(maxMass);
        return System.Math.Pow(10d, logMin + (logMax - logMin) * normalizedValue);
    }

    private static double GetNormalizedMassValue(CelestialBody planet, double massKg)
    {
        var minMass = planet.MassKg * MinimumMassFactor;
        var maxMass = planet.MassKg * MaximumMassFactor;
        var logMin = System.Math.Log10(minMass);
        var logMax = System.Math.Log10(maxMass);
        var logValue = System.Math.Log10(MathHelper.Clamp((float)massKg, (float)minMass, (float)maxMass));
        return (logValue - logMin) / (logMax - logMin);
    }

    private PhysicsVector2 GetFallbackTangentialDirection(PhysicalBody body)
    {
        var sunBody = GravitySimulation.GetBody(Sun.Name);
        var radial = sunBody is null ? body.PositionMeters : body.PositionMeters - sunBody.PositionMeters;
        var distance = radial.Length;

        if (distance <= 0.0001d)
            return new PhysicsVector2(0d, 1d);

        return new PhysicsVector2(-radial.Y / distance, radial.X / distance);
    }

    private void ResetTrails()
    {
        _planetTrails.Clear();

        foreach (var planet in Planets)
            _planetTrails[planet.Name] = new Queue<PhysicsVector2>();

        foreach (var satellite in Satellites)
            _planetTrails[satellite.Name] = new Queue<PhysicsVector2>();

        RecordTrailPoints();
    }

    private void RecordTrailPoints()
    {
        foreach (var planet in Planets)
        {
            var body = GravitySimulation.GetBody(planet.Name);
            if (body is null || !_planetTrails.TryGetValue(planet.Name, out var trail))
                continue;

            trail.Enqueue(body.PositionMeters);

            while (trail.Count > MaximumTrailPoints)
                trail.Dequeue();
        }

        foreach (var satellite in Satellites)
        {
            var body = GravitySimulation.GetBody(satellite.Name);
            if (body is null || !_planetTrails.TryGetValue(satellite.Name, out var trail))
                continue;

            trail.Enqueue(body.PositionMeters);

            while (trail.Count > MaximumTrailPoints)
                trail.Dequeue();
        }
    }
}

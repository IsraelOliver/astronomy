using System;
using System.Collections.Generic;

namespace Astronomia;

public sealed class GravitySimulation
{
    private const double MaximumStepSeconds = 3_600d;
    private const double SofteningMeters = 1_000_000d;

    private readonly List<PhysicalBody> _bodies;
    private readonly List<PhysicalBody> _initialBodies;

    public GravitySimulation(IReadOnlyList<PhysicalBody> bodies)
    {
        _bodies = CloneBodies(bodies);
        _initialBodies = CloneBodies(bodies);
    }

    public IReadOnlyList<PhysicalBody> Bodies => _bodies;

    public PhysicalBody? GetBody(string name)
    {
        foreach (var body in _bodies)
        {
            if (body.Name == name)
                return body;
        }

        return null;
    }

    public PhysicsVector2 GetCenterOfMass()
    {
        var weightedPosition = PhysicsVector2.Zero;
        var totalMass = 0d;

        foreach (var body in _bodies)
        {
            weightedPosition += body.PositionMeters * body.MassKg;
            totalMass += body.MassKg;
        }

        return totalMass > 0d ? weightedPosition / totalMass : PhysicsVector2.Zero;
    }

    public PhysicsVector2 GetAccelerationForBody(string name)
    {
        var body = GetBody(name);
        if (body is null)
            return PhysicsVector2.Zero;

        var acceleration = PhysicsVector2.Zero;

        foreach (var other in _bodies)
        {
            if (other == body)
                continue;

            var delta = other.PositionMeters - body.PositionMeters;
            var distanceSquared = delta.LengthSquared + SofteningMeters * SofteningMeters;
            var distance = Math.Sqrt(distanceSquared);
            var direction = delta / distance;
            acceleration += direction * (PhysicsConstants.GravitationalConstant * other.MassKg / distanceSquared);
        }

        return acceleration;
    }

    public void Reset()
    {
        _bodies.Clear();
        _bodies.AddRange(CloneBodies(_initialBodies));
    }

    public void Step(double elapsedSeconds)
    {
        var remainingSeconds = elapsedSeconds;

        while (remainingSeconds > 0d)
        {
            var stepSeconds = Math.Min(MaximumStepSeconds, remainingSeconds);
            Integrate(stepSeconds);
            remainingSeconds -= stepSeconds;
        }
    }

    private void Integrate(double deltaSeconds)
    {
        var currentAccelerations = CalculateAccelerations();

        for (var i = 0; i < _bodies.Count; i++)
        {
            var body = _bodies[i];
            body.PositionMeters += body.VelocityMetersPerSecond * deltaSeconds +
                currentAccelerations[i] * (0.5d * deltaSeconds * deltaSeconds);
        }

        var nextAccelerations = CalculateAccelerations();

        for (var i = 0; i < _bodies.Count; i++)
        {
            var body = _bodies[i];
            body.VelocityMetersPerSecond += (currentAccelerations[i] + nextAccelerations[i]) * (0.5d * deltaSeconds);
        }
    }

    private PhysicsVector2[] CalculateAccelerations()
    {
        var accelerations = new PhysicsVector2[_bodies.Count];

        // N-body: every body pulls every other body, including planet-planet interactions.
        for (var i = 0; i < _bodies.Count; i++)
        {
            for (var j = i + 1; j < _bodies.Count; j++)
            {
                var first = _bodies[i];
                var second = _bodies[j];
                var delta = second.PositionMeters - first.PositionMeters;
                var distanceSquared = delta.LengthSquared + SofteningMeters * SofteningMeters;
                var distance = Math.Sqrt(distanceSquared);
                var direction = delta / distance;

                accelerations[i] += direction * (PhysicsConstants.GravitationalConstant * second.MassKg / distanceSquared);
                accelerations[j] -= direction * (PhysicsConstants.GravitationalConstant * first.MassKg / distanceSquared);
            }
        }

        return accelerations;
    }

    private static List<PhysicalBody> CloneBodies(IReadOnlyList<PhysicalBody> bodies)
    {
        var clones = new List<PhysicalBody>();

        foreach (var body in bodies)
        {
            clones.Add(new PhysicalBody(
                body.Name,
                body.MassKg,
                body.PositionMeters,
                body.VelocityMetersPerSecond));
        }

        return clones;
    }
}

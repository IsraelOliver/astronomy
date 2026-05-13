namespace Astronomia;

public sealed class PhysicalBody
{
    public PhysicalBody(string name, double massKg, PhysicsVector2 positionMeters, PhysicsVector2 velocityMetersPerSecond)
    {
        Name = name;
        MassKg = massKg;
        PositionMeters = positionMeters;
        VelocityMetersPerSecond = velocityMetersPerSecond;
    }

    public string Name { get; }
    public double MassKg { get; set; }
    public PhysicsVector2 PositionMeters { get; set; }
    public PhysicsVector2 VelocityMetersPerSecond { get; set; }
}

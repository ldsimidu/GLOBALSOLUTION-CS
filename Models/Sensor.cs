namespace GlobalSolution.SenseSpot.API.Models;

public abstract class Sensor : BaseEntity
{
    public int DeviceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public SensorType SensorType { get; protected set; }
    public string Unit { get; protected set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Device? Device { get; set; }
    public ICollection<SensorReading> Readings { get; set; } = new List<SensorReading>();
}

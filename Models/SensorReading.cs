namespace GlobalSolution.SenseSpot.API.Models;

public class SensorReading : BaseEntity
{
    public int DeviceId { get; set; }
    public int SensorId { get; set; }
    public decimal Value { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsSynced { get; set; } = true;
    public ConnectionStatus ConnectionStatusAtCollection { get; set; }

    public Device? Device { get; set; }
    public Sensor? Sensor { get; set; }

    public void MarkAsSynced()
    {
        IsSynced = true;
        Touch();
    }
}

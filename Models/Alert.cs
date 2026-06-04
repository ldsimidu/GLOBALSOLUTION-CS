namespace GlobalSolution.SenseSpot.API.Models;

public class Alert : BaseEntity
{
    public int DeviceId { get; set; }
    public int? SensorReadingId { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }

    public Device? Device { get; set; }
}

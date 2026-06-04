namespace GlobalSolution.SenseSpot.API.Models;

public class Device : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string EnvironmentContext { get; set; } = string.Empty;
    public int BatteryLevel { get; set; }
    public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Online;
    public DateTime? LastReadingAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public DeviceConfiguration? Configuration { get; set; }
    public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    public ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    public ICollection<RiskAssessment> RiskAssessments { get; set; } = new List<RiskAssessment>();
    public ICollection<SyncLog> SyncLogs { get; set; } = new List<SyncLog>();

    public void UpdateStatus(int batteryLevel, ConnectionStatus connectionStatus)
    {
        BatteryLevel = Math.Clamp(batteryLevel, 0, 100);
        ConnectionStatus = connectionStatus;
        Touch();
    }

    public void RegisterReading(DateTime recordedAtUtc)
    {
        LastReadingAtUtc = recordedAtUtc;
        Touch();
    }
}

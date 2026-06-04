namespace GlobalSolution.SenseSpot.API.Models;

public class DeviceConfiguration : BaseEntity
{
    public int DeviceId { get; set; }
    public OperationMode OperationMode { get; set; } = OperationMode.Exploration;
    public int CollectionIntervalSeconds { get; set; } = 60;
    public decimal TemperatureAlertThreshold { get; set; } = 35m;
    public decimal HumidityAlertThreshold { get; set; } = 85m;
    public decimal LuminosityAlertThreshold { get; set; } = 20m;
    public decimal AirQualityAlertThreshold { get; set; } = 70m;
    public decimal VibrationAlertThreshold { get; set; } = 50m;

    public Device? Device { get; set; }
}

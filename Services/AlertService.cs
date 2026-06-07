using GlobalSolution.SenseSpot.API.Models;

namespace GlobalSolution.SenseSpot.API.Services;

public class AlertService : IAlertService
{
    public Alert? BuildAutomaticAlert(Device device, Sensor sensor, SensorReading reading)
    {
        var configuration = device.Configuration;
        if (configuration is null)
        {
            return null;
        }

        var threshold = sensor.SensorType switch
        {
            SensorType.Temperature => configuration.TemperatureAlertThreshold,
            SensorType.Humidity => configuration.HumidityAlertThreshold,
            SensorType.Luminosity => configuration.LuminosityAlertThreshold,
            SensorType.AirQuality => configuration.AirQualityAlertThreshold,
            SensorType.Vibration => configuration.VibrationAlertThreshold,
            _ => decimal.MaxValue
        };

        var triggered = sensor.SensorType == SensorType.Luminosity
            ? reading.Value <= threshold
            : reading.Value >= threshold;

        if (!triggered)
        {
            return null;
        }

        return new Alert
        {
            DeviceId = device.Id,
            SensorReadingId = reading.Id,
            Severity = sensor.SensorType is SensorType.AirQuality or SensorType.Vibration
                ? AlertSeverity.Critical
                : AlertSeverity.Warning,
            Message = $"{sensor.SensorType} reached a critical threshold with value {reading.Value} {sensor.Unit}."
        };
    }
}

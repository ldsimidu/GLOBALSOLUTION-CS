namespace GlobalSolution.SenseSpot.API.Models;

public record CreateDeviceRequest(
    string Name,
    string SerialNumber,
    string EnvironmentContext,
    int BatteryLevel,
    ConnectionStatus ConnectionStatus,
    OperationMode OperationMode,
    int CollectionIntervalSeconds);

public record UpdateDeviceStatusRequest(
    int BatteryLevel,
    ConnectionStatus ConnectionStatus,
    bool IsActive);

public record UpdateDeviceConfigurationRequest(
    OperationMode OperationMode,
    int CollectionIntervalSeconds,
    decimal TemperatureAlertThreshold,
    decimal HumidityAlertThreshold,
    decimal LuminosityAlertThreshold,
    decimal AirQualityAlertThreshold,
    decimal VibrationAlertThreshold);

public record AddSensorRequest(
    string Name,
    SensorType SensorType);

public record CreateSensorReadingRequest(
    int SensorId,
    decimal Value,
    DateTime RecordedAtUtc,
    ConnectionStatus ConnectionStatusAtCollection);

public record CreateAlertRequest(
    AlertSeverity Severity,
    string Message,
    DateTime? TriggeredAtUtc,
    int? SensorReadingId);

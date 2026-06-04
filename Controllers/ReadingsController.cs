using GlobalSolution.SenseSpot.API.Data;
using GlobalSolution.SenseSpot.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobalSolution.SenseSpot.API.Controllers;

[ApiController]
[Route("api/devices/{deviceId:int}/readings")]
public class ReadingsController(AppDbContext context) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<object>> CreateReading(int deviceId, CreateSensorReadingRequest request)
    {
        var device = await context.Devices
            .Include(x => x.Configuration)
            .FirstOrDefaultAsync(x => x.Id == deviceId);

        if (device is null)
        {
            return NotFound("Device not found.");
        }

        var sensor = await context.Sensors.FirstOrDefaultAsync(x => x.Id == request.SensorId && x.DeviceId == deviceId);
        if (sensor is null)
        {
            return NotFound("Sensor not found for this device.");
        }

        var now = DateTime.UtcNow;
        if (request.RecordedAtUtc > now.AddMinutes(5))
        {
            return BadRequest("RecordedAtUtc cannot be in the future.");
        }

        var isSynced = request.ConnectionStatusAtCollection != ConnectionStatus.Offline;
        var reading = new SensorReading
        {
            DeviceId = deviceId,
            SensorId = sensor.Id,
            Value = request.Value,
            RecordedAtUtc = DateTime.SpecifyKind(request.RecordedAtUtc, DateTimeKind.Utc),
            ReceivedAtUtc = now,
            IsSynced = isSynced,
            ConnectionStatusAtCollection = request.ConnectionStatusAtCollection
        };

        context.SensorReadings.Add(reading);
        device.RegisterReading(reading.RecordedAtUtc);

        var alert = BuildAlertIfNeeded(device, sensor, reading);
        if (alert is not null)
        {
            context.Alerts.Add(alert);
        }

        var assessment = await BuildRiskAssessmentAsync(deviceId, device.Configuration);
        context.RiskAssessments.Add(assessment);

        if (!isSynced)
        {
            context.SyncLogs.Add(new SyncLog
            {
                DeviceId = deviceId,
                PendingReadingsCount = await context.SensorReadings.CountAsync(x => x.DeviceId == deviceId && !x.IsSynced) + 1,
                Action = "ReadingStoredOffline",
                Status = "PendingSync",
                Details = "Reading stored locally until connectivity is restored."
            });
        }

        await context.SaveChangesAsync();

        return Created(string.Empty, new
        {
            reading.Id,
            reading.DeviceId,
            reading.SensorId,
            reading.Value,
            reading.RecordedAtUtc,
            reading.ReceivedAtUtc,
            reading.IsSynced,
            LatestRisk = assessment.Classification,
            assessment.RecommendedAction
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetReadings(
        int deviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var deviceExists = await context.Devices
            .CountAsync(x => x.Id == deviceId) > 0;
        if (!deviceExists)
        {
            return NotFound("Device not found.");
        }

        var query = context.SensorReadings
            .Include(x => x.Sensor)
            .Where(x => x.DeviceId == deviceId)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(x => x.RecordedAtUtc >= from.Value.ToUniversalTime());
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.RecordedAtUtc <= to.Value.ToUniversalTime());
        }

        var readings = await query
            .OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.SensorId,
                SensorName = x.Sensor!.Name,
                x.Value,
                x.RecordedAtUtc,
                x.ReceivedAtUtc,
                x.IsSynced,
                x.ConnectionStatusAtCollection
            })
            .ToListAsync();

        return Ok(readings);
    }

    [HttpPost("/api/devices/{deviceId:int}/sync")]
    public async Task<ActionResult<object>> SyncPendingReadings(int deviceId)
    {
        var device = await context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId);
        if (device is null)
        {
            return NotFound("Device not found.");
        }

        var pendingReadings = await context.SensorReadings
            .Where(x => x.DeviceId == deviceId && !x.IsSynced)
            .ToListAsync();

        foreach (var reading in pendingReadings)
        {
            reading.MarkAsSynced();
        }

        context.SyncLogs.Add(new SyncLog
        {
            DeviceId = deviceId,
            PendingReadingsCount = pendingReadings.Count,
            Action = "ManualSync",
            Status = "Completed",
            Details = pendingReadings.Count == 0
                ? "No pending readings were found."
                : $"{pendingReadings.Count} pending readings synchronized."
        });

        device.ConnectionStatus = ConnectionStatus.Online;
        device.Touch();

        await context.SaveChangesAsync();

        return Ok(new
        {
            DeviceId = deviceId,
            SynchronizedReadings = pendingReadings.Count
        });
    }

    [HttpGet("/api/devices/{deviceId:int}/risk-assessment")]
    public async Task<ActionResult<object>> GetLatestRiskAssessment(int deviceId)
    {
        var assessment = await context.RiskAssessments
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.AssessedAtUtc)
            .FirstOrDefaultAsync();

        if (assessment is null)
        {
            return NotFound("No risk assessment found for this device.");
        }

        return Ok(assessment);
    }

    [HttpGet("/api/devices/{deviceId:int}/alerts")]
    public async Task<ActionResult<IEnumerable<Alert>>> GetAlerts(int deviceId)
    {
        var deviceExists = await context.Devices
            .CountAsync(x => x.Id == deviceId) > 0;
        if (!deviceExists)
        {
            return NotFound("Device not found.");
        }

        var alerts = await context.Alerts
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.TriggeredAtUtc)
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpGet("/api/devices/{deviceId:int}/sync-logs")]
    public async Task<ActionResult<IEnumerable<SyncLog>>> GetSyncLogs(int deviceId)
    {
        var deviceExists = await context.Devices
            .CountAsync(x => x.Id == deviceId) > 0;
        if (!deviceExists)
        {
            return NotFound("Device not found.");
        }

        var logs = await context.SyncLogs
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToListAsync();

        return Ok(logs);
    }

    private Alert? BuildAlertIfNeeded(Device device, Sensor sensor, SensorReading reading)
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

    private async Task<RiskAssessment> BuildRiskAssessmentAsync(int deviceId, DeviceConfiguration? configuration)
    {
        var latestReadings = await context.SensorReadings
            .Include(x => x.Sensor)
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(10)
            .ToListAsync();

        if (configuration is null || latestReadings.Count == 0)
        {
            return new RiskAssessment
            {
                DeviceId = deviceId,
                Classification = RiskLevel.Safe,
                Summary = "Insufficient data to detect environmental anomalies.",
                RecommendedAction = "Advance with monitoring.",
                PrimaryRiskFactors = "No significant risk factors detected."
            };
        }

        var criticalFactors = new List<string>();
        var attentionFactors = new List<string>();

        foreach (var reading in latestReadings)
        {
            var sensorType = reading.Sensor!.SensorType;
            var value = reading.Value;

            switch (sensorType)
            {
                case SensorType.AirQuality when value >= configuration.AirQualityAlertThreshold:
                    criticalFactors.Add("air quality");
                    break;
                case SensorType.Vibration when value >= configuration.VibrationAlertThreshold:
                    criticalFactors.Add("vibration");
                    break;
                case SensorType.Temperature when value >= configuration.TemperatureAlertThreshold:
                    attentionFactors.Add("temperature");
                    break;
                case SensorType.Humidity when value >= configuration.HumidityAlertThreshold:
                    attentionFactors.Add("humidity");
                    break;
                case SensorType.Luminosity when value <= configuration.LuminosityAlertThreshold:
                    attentionFactors.Add("luminosity");
                    break;
            }
        }

        RiskLevel classification;
        string summary;
        string action;
        string factors;

        if (criticalFactors.Count > 0)
        {
            classification = RiskLevel.Critical;
            factors = string.Join(", ", criticalFactors.Distinct());
            summary = $"Critical environment detected due to {factors}.";
            action = "Avoid human entry and maintain remote operation.";
        }
        else if (attentionFactors.Count > 0)
        {
            classification = RiskLevel.Attention;
            factors = string.Join(", ", attentionFactors.Distinct());
            summary = $"Environmental attention required due to {factors}.";
            action = "Pause, reassess conditions, and continue with caution.";
        }
        else
        {
            classification = RiskLevel.Safe;
            factors = "No significant risk factors detected.";
            summary = "Environment is within expected operating thresholds.";
            action = "Advance with monitoring.";
        }

        return new RiskAssessment
        {
            DeviceId = deviceId,
            Classification = classification,
            Summary = summary,
            RecommendedAction = action,
            PrimaryRiskFactors = factors
        };
    }
}

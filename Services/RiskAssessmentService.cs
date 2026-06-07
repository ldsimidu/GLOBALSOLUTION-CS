using GlobalSolution.SenseSpot.API.Data;
using GlobalSolution.SenseSpot.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GlobalSolution.SenseSpot.API.Services;

public class RiskAssessmentService(AppDbContext context) : IRiskAssessmentService
{
    public async Task<RiskAssessment> BuildLatestAssessmentAsync(
        int deviceId,
        DeviceConfiguration? configuration,
        CancellationToken cancellationToken = default)
    {
        var latestReadings = await context.SensorReadings
            .Include(x => x.Sensor)
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

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

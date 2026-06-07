using GlobalSolution.SenseSpot.API.Data;
using GlobalSolution.SenseSpot.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobalSolution.SenseSpot.API.Controllers;

/// <summary>
/// Gerencia os gadgets BrightSpot, suas configuracoes operacionais e os sensores acoplados.
/// </summary>
[ApiController]
[Route("api/devices")]
public class DevicesController(AppDbContext context) : ControllerBase
{
    /// <summary>
    /// Cadastra um novo gadget BrightSpot.
    /// </summary>
    /// <remarks>
    /// Use esta rota para registrar um dispositivo que sera enviado a um ambiente hostil.
    /// O foco da requisicao e criar a identidade do gadget e sua configuracao operacional inicial.
    /// </remarks>
    [HttpPost]
    public async Task<ActionResult<object>> CreateDevice(CreateDeviceRequest request)
    {
        if (request.CollectionIntervalSeconds <= 0)
        {
            return BadRequest("Collection interval must be greater than zero.");
        }

        var serialExists = await context.Devices
            .CountAsync(x => x.SerialNumber == request.SerialNumber) > 0;
        if (serialExists)
        {
            return Conflict("A device with this serial number already exists.");
        }

        var device = new Device
        {
            Name = request.Name.Trim(),
            SerialNumber = request.SerialNumber.Trim(),
            EnvironmentContext = request.EnvironmentContext.Trim(),
            BatteryLevel = Math.Clamp(request.BatteryLevel, 0, 100),
            ConnectionStatus = request.ConnectionStatus,
            Configuration = new DeviceConfiguration
            {
                OperationMode = request.OperationMode,
                CollectionIntervalSeconds = request.CollectionIntervalSeconds
            }
        };

        context.Devices.Add(device);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDeviceById), new { id = device.Id }, MapDeviceSummary(device));
    }

    /// <summary>
    /// Lista todos os gadgets cadastrados.
    /// </summary>
    /// <remarks>
    /// Retorna uma visao resumida dos dispositivos para o dashboard.
    /// O foco desta rota e mostrar status basico, quantidade de sensores e configuracao atual de cada gadget.
    /// </remarks>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetDevices()
    {
        var devices = await context.Devices
            .OrderBy(x => x.Name)
            .ToListAsync();

        var deviceIds = devices.Select(x => x.Id).ToList();

        var configurations = await context.DeviceConfigurations
            .Where(x => deviceIds.Contains(x.DeviceId))
            .ToListAsync();

        var sensorCounts = await context.Sensors
            .Where(x => deviceIds.Contains(x.DeviceId))
            .GroupBy(x => x.DeviceId)
            .Select(x => new
            {
                DeviceId = x.Key,
                Count = x.Count()
            })
            .ToListAsync();

        return Ok(devices.Select(x =>
        {
            var configuration = configurations.FirstOrDefault(c => c.DeviceId == x.Id);
            var sensorCount = sensorCounts.FirstOrDefault(s => s.DeviceId == x.Id)?.Count ?? 0;

            return new
            {
                x.Id,
                x.Name,
                x.SerialNumber,
                x.EnvironmentContext,
                x.BatteryLevel,
                x.ConnectionStatus,
                x.LastReadingAtUtc,
                x.IsActive,
                SensorCount = sensorCount,
                Configuration = configuration == null
                    ? null
                    : new
                    {
                        configuration.OperationMode,
                        configuration.CollectionIntervalSeconds
                    }
            };
        }));
    }

    /// <summary>
    /// Busca o detalhe completo de um gadget.
    /// </summary>
    /// <remarks>
    /// Retorna informacoes detalhadas do dispositivo, incluindo configuracao, sensores, ultimo risco calculado e alertas recentes.
    /// O foco desta rota e alimentar a tela de detalhe do gadget no dashboard.
    /// </remarks>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetDeviceById(int id)
    {
        var device = await context.Devices
            .Include(x => x.Configuration)
            .Include(x => x.Sensors)
            .Include(x => x.Alerts.OrderByDescending(a => a.TriggeredAtUtc).Take(5))
            .Include(x => x.RiskAssessments.OrderByDescending(r => r.AssessedAtUtc).Take(1))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (device is null)
        {
            return NotFound("Device not found.");
        }

        return Ok(new
        {
            device.Id,
            device.Name,
            device.SerialNumber,
            device.EnvironmentContext,
            device.BatteryLevel,
            device.ConnectionStatus,
            device.LastReadingAtUtc,
            device.IsActive,
            Configuration = device.Configuration == null
                ? null
                : new
                {
                    device.Configuration.Id,
                    device.Configuration.DeviceId,
                    device.Configuration.OperationMode,
                    device.Configuration.CollectionIntervalSeconds,
                    device.Configuration.TemperatureAlertThreshold,
                    device.Configuration.HumidityAlertThreshold,
                    device.Configuration.LuminosityAlertThreshold,
                    device.Configuration.AirQualityAlertThreshold,
                    device.Configuration.VibrationAlertThreshold
                },
            Sensors = device.Sensors.Select(s => new
            {
                s.Id,
                s.Name,
                s.SensorType,
                s.Unit,
                s.IsActive
            }),
            LatestRiskAssessment = device.RiskAssessments
                .OrderByDescending(x => x.AssessedAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.DeviceId,
                    x.Classification,
                    x.Summary,
                    x.RecommendedAction,
                    x.PrimaryRiskFactors,
                    x.AssessedAtUtc
                })
                .FirstOrDefault(),
            RecentAlerts = device.Alerts
                .OrderByDescending(x => x.TriggeredAtUtc)
                .Take(5)
                .Select(x => new
                {
                    x.Id,
                    x.DeviceId,
                    x.SensorReadingId,
                    x.Severity,
                    x.Message,
                    x.TriggeredAtUtc,
                    x.IsAcknowledged
                })
        });
    }

    /// <summary>
    /// Atualiza o status operacional de um gadget.
    /// </summary>
    /// <remarks>
    /// Use esta rota para informar mudancas de bateria, conectividade e disponibilidade do dispositivo.
    /// O foco e refletir o estado atual do gadget em campo.
    /// </remarks>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<object>> UpdateStatus(int id, UpdateDeviceStatusRequest request)
    {
        var device = await context.Devices.FirstOrDefaultAsync(x => x.Id == id);
        if (device is null)
        {
            return NotFound("Device not found.");
        }

        device.UpdateStatus(request.BatteryLevel, request.ConnectionStatus);
        device.IsActive = request.IsActive;

        await context.SaveChangesAsync();
        return Ok(MapDeviceSummary(device));
    }

    /// <summary>
    /// Atualiza a configuracao operacional do gadget.
    /// </summary>
    /// <remarks>
    /// Aqui sao definidos modo de operacao, intervalo de coleta e limites de alerta por sensor.
    /// O foco desta rota e controlar como o BrightSpot interpreta e reage ao ambiente.
    /// </remarks>
    [HttpPatch("{id:int}/configuration")]
    public async Task<ActionResult<object>> UpdateConfiguration(int id, UpdateDeviceConfigurationRequest request)
    {
        if (request.CollectionIntervalSeconds <= 0)
        {
            return BadRequest("Collection interval must be greater than zero.");
        }

        var device = await context.Devices
            .Include(x => x.Configuration)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (device is null)
        {
            return NotFound("Device not found.");
        }

        if (device.Configuration is null)
        {
            device.Configuration = new DeviceConfiguration { DeviceId = device.Id };
        }

        device.Configuration.OperationMode = request.OperationMode;
        device.Configuration.CollectionIntervalSeconds = request.CollectionIntervalSeconds;
        device.Configuration.TemperatureAlertThreshold = request.TemperatureAlertThreshold;
        device.Configuration.HumidityAlertThreshold = request.HumidityAlertThreshold;
        device.Configuration.LuminosityAlertThreshold = request.LuminosityAlertThreshold;
        device.Configuration.AirQualityAlertThreshold = request.AirQualityAlertThreshold;
        device.Configuration.VibrationAlertThreshold = request.VibrationAlertThreshold;
        device.Configuration.Touch();
        device.Touch();

        await context.SaveChangesAsync();
        return Ok(MapDeviceSummary(device));
    }

    /// <summary>
    /// Acopla um sensor a um gadget.
    /// </summary>
    /// <remarks>
    /// Use esta rota para registrar quais sensores ambientais estao disponiveis em cada dispositivo.
    /// O foco e montar a capacidade de leitura do gadget antes da operacao.
    /// </remarks>
    [HttpPost("{id:int}/sensors")]
    public async Task<ActionResult<object>> AddSensor(int id, AddSensorRequest request)
    {
        var device = await context.Devices.FirstOrDefaultAsync(x => x.Id == id);
        if (device is null)
        {
            return NotFound("Device not found.");
        }

        Sensor? sensor = request.SensorType switch
        {
            SensorType.Temperature => new TemperatureSensor(),
            SensorType.Humidity => new HumiditySensor(),
            SensorType.Luminosity => new LuminositySensor(),
            SensorType.AirQuality => new AirQualitySensor(),
            SensorType.Vibration => new VibrationSensor(),
            _ => null
        };

        if (sensor is null)
        {
            return BadRequest("Unsupported sensor type.");
        }

        sensor.Name = request.Name.Trim();
        sensor.DeviceId = device.Id;

        context.Sensors.Add(sensor);
        await context.SaveChangesAsync();

        return Created($"/api/devices/{id}/sensors/{sensor.Id}", new
        {
            sensor.Id,
            sensor.Name,
            sensor.SensorType,
            sensor.Unit,
            sensor.DeviceId
        });
    }

    /// <summary>
    /// Lista os sensores acoplados a um gadget.
    /// </summary>
    /// <remarks>
    /// Retorna os sensores ativos do dispositivo, como temperatura, umidade, luminosidade, qualidade do ar e vibracao.
    /// O foco desta rota e mostrar a composicao de leitura do gadget.
    /// </remarks>
    [HttpGet("{id:int}/sensors")]
    public async Task<ActionResult<IEnumerable<object>>> GetSensors(int id)
    {
        var deviceExists = await context.Devices
            .CountAsync(x => x.Id == id) > 0;
        if (!deviceExists)
        {
            return NotFound("Device not found.");
        }

        var sensors = await context.Sensors
            .Where(x => x.DeviceId == id)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(sensors.Select(x => new
        {
            x.Id,
            x.Name,
            x.SensorType,
            x.Unit,
            x.IsActive
        }));
    }

    /// <summary>
    /// Remove um gadget BrightSpot e todo o historico operacional vinculado a ele.
    /// </summary>
    /// <remarks>
    /// Use esta rota quando um dispositivo for descartado da operacao.
    /// A remocao inclui configuracao, sensores, leituras, alertas, avaliacoes de risco e logs de sincronizacao.
    /// </remarks>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        var device = await context.Devices.FirstOrDefaultAsync(x => x.Id == id);
        if (device is null)
        {
            return NotFound("Device not found.");
        }

        var alerts = await context.Alerts.Where(x => x.DeviceId == id).ToListAsync();
        var riskAssessments = await context.RiskAssessments.Where(x => x.DeviceId == id).ToListAsync();
        var syncLogs = await context.SyncLogs.Where(x => x.DeviceId == id).ToListAsync();
        var readings = await context.SensorReadings.Where(x => x.DeviceId == id).ToListAsync();
        var sensors = await context.Sensors.Where(x => x.DeviceId == id).ToListAsync();
        var configuration = await context.DeviceConfigurations.FirstOrDefaultAsync(x => x.DeviceId == id);

        context.Alerts.RemoveRange(alerts);
        context.RiskAssessments.RemoveRange(riskAssessments);
        context.SyncLogs.RemoveRange(syncLogs);
        context.SensorReadings.RemoveRange(readings);
        context.Sensors.RemoveRange(sensors);

        if (configuration is not null)
        {
            context.DeviceConfigurations.Remove(configuration);
        }

        context.Devices.Remove(device);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private static object MapDeviceSummary(Device device)
    {
        return new
        {
            device.Id,
            device.Name,
            device.SerialNumber,
            device.EnvironmentContext,
            device.BatteryLevel,
            device.ConnectionStatus,
            device.LastReadingAtUtc,
            device.IsActive,
            Configuration = device.Configuration == null
                ? null
                : new
                {
                    device.Configuration.Id,
                    device.Configuration.DeviceId,
                    device.Configuration.OperationMode,
                    device.Configuration.CollectionIntervalSeconds,
                    device.Configuration.TemperatureAlertThreshold,
                    device.Configuration.HumidityAlertThreshold,
                    device.Configuration.LuminosityAlertThreshold,
                    device.Configuration.AirQualityAlertThreshold,
                    device.Configuration.VibrationAlertThreshold
                }
        };
    }
}

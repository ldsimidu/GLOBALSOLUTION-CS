using GlobalSolution.SenseSpot.API.Data;
using GlobalSolution.SenseSpot.API.Models;
using GlobalSolution.SenseSpot.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobalSolution.SenseSpot.API.Controllers;

/// <summary>
/// Gerencia leituras ambientais, sincronizacao offline, analise de risco e alertas do BrightSpot.
/// </summary>
[ApiController]
[Route("api/devices/{deviceId:int}/readings")]
public class ReadingsController(
    AppDbContext context,
    IAlertService alertService,
    IRiskAssessmentService riskAssessmentService) : ControllerBase
{
    /// <summary>
    /// Registra uma nova leitura ambiental para um gadget.
    /// </summary>
    /// <remarks>
    /// Use esta rota quando o dispositivo coletar um valor de sensor.
    /// O foco e armazenar a leitura, atualizar o historico do gadget, gerar alertas e recalcular o risco ambiental.
    /// </remarks>
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

        var alert = alertService.BuildAutomaticAlert(device, sensor, reading);
        if (alert is not null)
        {
            context.Alerts.Add(alert);
        }

        var assessment = await riskAssessmentService.BuildLatestAssessmentAsync(deviceId, device.Configuration);
        context.RiskAssessments.Add(assessment);

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

    /// <summary>
    /// Lista o historico de leituras ambientais de um gadget.
    /// </summary>
    /// <remarks>
    /// Pode ser filtrada por periodo usando os parametros <c>from</c> e <c>to</c>.
    /// O foco desta rota e consultar a caixa-preta ambiental e o historico operacional do dispositivo.
    /// </remarks>
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

    /// <summary>
    /// Sincroniza leituras pendentes armazenadas offline.
    /// </summary>
    /// <remarks>
    /// Use esta rota quando o gadget recuperar conectividade.
    /// O foco e marcar leituras como sincronizadas e registrar o evento de envio posterior ao servidor.
    /// </remarks>
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

    /// <summary>
    /// Retorna a avaliacao de risco mais recente de um gadget.
    /// </summary>
    /// <remarks>
    /// O foco desta rota e mostrar a interpretacao consolidada do ambiente, incluindo classificacao, resumo e acao recomendada.
    /// </remarks>
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

    /// <summary>
    /// Lista os alertas de um gadget.
    /// </summary>
    /// <remarks>
    /// Retorna eventos de anomalia gerados quando leituras ultrapassam limites configurados.
    /// O foco desta rota e destacar ocorrencias que exigem atencao operacional.
    /// </remarks>
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

    /// <summary>
    /// Cadastra um alerta manual para um gadget.
    /// </summary>
    /// <remarks>
    /// Use esta rota para registrar alertas operacionais que nao nasceram automaticamente de uma leitura,
    /// como bloqueio de rota, dano fisico no gadget ou recomendacao de retirada preventiva.
    /// Quando informado, o <c>SensorReadingId</c> precisa pertencer ao mesmo gadget.
    /// </remarks>
    [HttpPost("/api/devices/{deviceId:int}/alerts")]
    public async Task<ActionResult<object>> CreateAlert(int deviceId, CreateAlertRequest request)
    {
        var deviceExists = await context.Devices
            .CountAsync(x => x.Id == deviceId) > 0;
        if (!deviceExists)
        {
            return NotFound("Device not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Alert message is required.");
        }

        var triggeredAtUtc = request.TriggeredAtUtc.HasValue
            ? DateTime.SpecifyKind(request.TriggeredAtUtc.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        if (triggeredAtUtc > DateTime.UtcNow.AddMinutes(5))
        {
            return BadRequest("TriggeredAtUtc cannot be in the future.");
        }

        if (request.SensorReadingId.HasValue)
        {
            var readingExists = await context.SensorReadings
                .CountAsync(x => x.Id == request.SensorReadingId.Value && x.DeviceId == deviceId) > 0;
            if (!readingExists)
            {
                return BadRequest("SensorReadingId does not belong to this device.");
            }
        }

        var alert = new Alert
        {
            DeviceId = deviceId,
            SensorReadingId = request.SensorReadingId,
            Severity = request.Severity,
            Message = request.Message.Trim(),
            TriggeredAtUtc = triggeredAtUtc
        };

        context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        return Created($"/api/devices/{deviceId}/alerts/{alert.Id}", new
        {
            alert.Id,
            alert.DeviceId,
            alert.SensorReadingId,
            alert.Severity,
            alert.Message,
            alert.TriggeredAtUtc,
            alert.IsAcknowledged
        });
    }

    /// <summary>
    /// Remove um alerta especifico de um gadget.
    /// </summary>
    /// <remarks>
    /// Use esta rota para limpar alertas descartados pela operacao depois da analise do evento.
    /// A leitura original permanece preservada no historico ambiental do gadget.
    /// </remarks>
    [HttpDelete("/api/devices/{deviceId:int}/alerts/{alertId:int}")]
    public async Task<IActionResult> DeleteAlert(int deviceId, int alertId)
    {
        var alert = await context.Alerts
            .FirstOrDefaultAsync(x => x.Id == alertId && x.DeviceId == deviceId);

        if (alert is null)
        {
            return NotFound("Alert not found for this device.");
        }

        context.Alerts.Remove(alert);
        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Lista o historico de sincronizacoes do gadget.
    /// </summary>
    /// <remarks>
    /// <c>sync-logs</c> registra quando o dispositivo guardou leituras offline ou quando enviou dados pendentes ao servidor.
    /// O foco desta rota e provar rastreabilidade da caixa-preta ambiental e do fluxo offline/online do BrightSpot.
    /// </remarks>
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

}

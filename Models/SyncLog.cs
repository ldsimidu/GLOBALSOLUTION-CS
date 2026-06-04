namespace GlobalSolution.SenseSpot.API.Models;

public class SyncLog : BaseEntity
{
    public int DeviceId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public int PendingReadingsCount { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;

    public Device? Device { get; set; }
}

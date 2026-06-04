namespace GlobalSolution.SenseSpot.API.Models;

public class RiskAssessment : BaseEntity
{
    public int DeviceId { get; set; }
    public RiskLevel Classification { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public string PrimaryRiskFactors { get; set; } = string.Empty;
    public DateTime AssessedAtUtc { get; set; } = DateTime.UtcNow;

    public Device? Device { get; set; }
}

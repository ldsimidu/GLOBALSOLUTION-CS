using GlobalSolution.SenseSpot.API.Models;

namespace GlobalSolution.SenseSpot.API.Services;

public interface IRiskAssessmentService
{
    Task<RiskAssessment> BuildLatestAssessmentAsync(
        int deviceId,
        DeviceConfiguration? configuration,
        CancellationToken cancellationToken = default);
}

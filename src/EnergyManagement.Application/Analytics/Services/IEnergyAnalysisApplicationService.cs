using EnergyManagement.Core.Common;

namespace EnergyManagement.Application.Analytics.Services;

public interface IEnergyAnalysisApplicationService
{
    Task<Result<EnergyDashboardDto>> GetDashboardAnalyticsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<Result<IReadOnlyList<EnergyPatternDto>>> GetConsumptionPatternsAsync(int userId);
    Task<Result<IReadOnlyList<AnomalyDetectionDto>>> DetectAnomaliesAsync(int userId, int days = 30);
    Task<Result<EnergyForecastDto>> GetEnergyForecastAsync(int userId, int days = 7);
    Task<Result<IReadOnlyList<DeviceRankingDto>>> GetDeviceRankingAsync(int userId);
}

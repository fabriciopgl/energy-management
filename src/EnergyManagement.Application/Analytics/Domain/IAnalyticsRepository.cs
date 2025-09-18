namespace EnergyManagement.Application.Analytics.Domain;

public interface IAnalyticsRepository
{
    Task<IReadOnlyList<ConsumptionPattern>> GetUserPatternsAsync(int userId);
    Task SavePatternAsync(ConsumptionPattern pattern);
    Task<IReadOnlyList<AnomalyDetection>> GetUserAnomaliesAsync(int userId, bool includeResolved = false);
    Task SaveAnomalyAsync(AnomalyDetection anomaly);
    Task<IReadOnlyList<EnergyRecommendation>> GetUserRecommendationsAsync(int userId, bool includeApplied = false);
    Task SaveRecommendationAsync(EnergyRecommendation recommendation);
    Task MarkAnomalyAsResolvedAsync(int anomalyId);
    Task MarkRecommendationAsAppliedAsync(int recommendationId);
}

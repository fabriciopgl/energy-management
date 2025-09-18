using EnergyManagement.Application.Analytics.Models.Dtos;
using EnergyManagement.Application.Analytics.Models.Requests;
using EnergyManagement.Core.Common;

namespace EnergyManagement.Application.Analytics.Services;

public interface IAnalyticsApplicationService
{
    Task<Result<EnergyAnalysisDto>> AnalyzeConsumptionAsync(int userId, AnalyzeConsumptionRequest request);
    Task<Result<List<ConsumptionPatternDto>>> GetConsumptionPatternsAsync(int userId);
    Task<Result<List<AnomalyDetectionDto>>> GetAnomaliesAsync(int userId, bool includeResolved = false);
    Task<Result<List<EnergyRecommendationDto>>> GetRecommendationsAsync(int userId, bool includeApplied = false);
    Task<Result> ResolveAnomalyAsync(int userId, ResolveAnomalyRequest request);
    Task<Result> ApplyRecommendationAsync(int userId, int recommendationId);
    Task<Result> RunAnalysisAsync(int userId);
}
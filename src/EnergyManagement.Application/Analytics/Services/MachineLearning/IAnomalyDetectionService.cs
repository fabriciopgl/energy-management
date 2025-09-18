using EnergyManagement.Application.Analytics.Models.Dtos;

namespace EnergyManagement.Application.Analytics.Services.MachineLearning;

public interface IAnomalyDetectionService
{
    Task<List<AnomalyDetectionDto>> DetectAnomaliesAsync(int userId, DateTime startDate, DateTime endDate);
    Task<double> CalculateAnomalyScoreAsync(double value, List<double> historicalValues);
}

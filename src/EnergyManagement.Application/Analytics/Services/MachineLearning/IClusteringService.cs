using EnergyManagement.Application.Analytics.Models.Dtos;

namespace EnergyManagement.Application.Analytics.Services.MachineLearning;

public interface IClusteringService
{
    Task<List<ConsumptionPatternDto>> IdentifyConsumptionPatternsAsync(int userId, DateTime startDate, DateTime endDate);
    Task<Dictionary<int, string>> GetClusterLabelsAsync();
}
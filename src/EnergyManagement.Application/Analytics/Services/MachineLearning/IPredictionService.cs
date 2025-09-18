using EnergyManagement.Application.Analytics.Models.Dtos;

namespace EnergyManagement.Application.Analytics.Services.MachineLearning;

public interface IPredictionService
{
    Task<List<HourlyConsumptionDto>> PredictHourlyConsumptionAsync(int userId, int hoursAhead = 24);
    Task<List<DailyConsumptionDto>> PredictDailyConsumptionAsync(int userId, int daysAhead = 7);
    Task<double> PredictNextHourConsumptionAsync(int userId);
}

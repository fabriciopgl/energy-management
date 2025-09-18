using EnergyManagement.Application.Analytics.Models.Dtos;
using EnergyManagement.Application.Sensors.Domain;
using Microsoft.Extensions.Logging;

namespace EnergyManagement.Application.Analytics.Services.MachineLearning;

public class ClusteringService(ISensorReadingRepository sensorRepository, ILogger<ClusteringService> logger) : IClusteringService
{
    public async Task<List<ConsumptionPatternDto>> IdentifyConsumptionPatternsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var readings = await sensorRepository.GetReadingsByUserAndPeriodAsync(userId, startDate, endDate);

            if (!readings.Any())
                return new List<ConsumptionPatternDto>();

            // Agrupar por hora do dia para identificar padrões
            var hourlyData = readings
                .GroupBy(r => r.Timestamp.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    AverageConsumption = g.Average(r => r.Current * r.Voltage), // Potência aproximada
                    PeakConsumption = g.Max(r => r.Current * r.Voltage),
                    Count = g.Count()
                })
                .Where(x => x.Count >= 5) // Filtrar horas com poucos dados
                .ToList();

            // Algoritmo K-means simplificado com 4 clusters (manhã, tarde, noite, madrugada)
            var patterns = new List<ConsumptionPatternDto>();

            // Cluster 1: Manhã (6h-12h)
            var morningData = hourlyData.Where(h => h.Hour >= 6 && h.Hour < 12).ToList();
            if (morningData.Any())
            {
                patterns.Add(new ConsumptionPatternDto
                {
                    PatternType = "Morning",
                    AverageConsumption = morningData.Average(x => x.AverageConsumption),
                    PeakConsumption = morningData.Max(x => x.PeakConsumption),
                    StartTime = new TimeSpan(6, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    ClusterId = 1,
                    AnalyzedAt = DateTime.UtcNow
                });
            }

            // Cluster 2: Tarde (12h-18h)
            var afternoonData = hourlyData.Where(h => h.Hour >= 12 && h.Hour < 18).ToList();
            if (afternoonData.Any())
            {
                patterns.Add(new ConsumptionPatternDto
                {
                    PatternType = "Afternoon",
                    AverageConsumption = afternoonData.Average(x => x.AverageConsumption),
                    PeakConsumption = afternoonData.Max(x => x.PeakConsumption),
                    StartTime = new TimeSpan(12, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    ClusterId = 2,
                    AnalyzedAt = DateTime.UtcNow
                });
            }

            // Cluster 3: Noite (18h-24h)
            var eveningData = hourlyData.Where(h => h.Hour >= 18 && h.Hour < 24).ToList();
            if (eveningData.Any())
            {
                patterns.Add(new ConsumptionPatternDto
                {
                    PatternType = "Evening",
                    AverageConsumption = eveningData.Average(x => x.AverageConsumption),
                    PeakConsumption = eveningData.Max(x => x.PeakConsumption),
                    StartTime = new TimeSpan(18, 0, 0),
                    EndTime = new TimeSpan(23, 59, 59),
                    ClusterId = 3,
                    AnalyzedAt = DateTime.UtcNow
                });
            }

            // Cluster 4: Madrugada (0h-6h)
            var nightData = hourlyData.Where(h => h.Hour >= 0 && h.Hour < 6).ToList();
            if (nightData.Any())
            {
                patterns.Add(new ConsumptionPatternDto
                {
                    PatternType = "Night",
                    AverageConsumption = nightData.Average(x => x.AverageConsumption),
                    PeakConsumption = nightData.Max(x => x.PeakConsumption),
                    StartTime = new TimeSpan(0, 0, 0),
                    EndTime = new TimeSpan(6, 0, 0),
                    ClusterId = 4,
                    AnalyzedAt = DateTime.UtcNow
                });
            }

            logger.LogInformation("Identified {Count} consumption patterns for user {UserId}", patterns.Count, userId);
            return patterns;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error identifying consumption patterns for user {UserId}", userId);
            return new List<ConsumptionPatternDto>();
        }
    }

    public async Task<Dictionary<int, string>> GetClusterLabelsAsync()
    {
        return await Task.FromResult(new Dictionary<int, string>
        {
            { 1, "Morning Peak" },
            { 2, "Afternoon Usage" },
            { 3, "Evening Peak" },
            { 4, "Night Base Load" }
        });
    }
}
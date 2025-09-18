using EnergyManagement.Application.Analytics.Models.Dtos;
using EnergyManagement.Application.Sensors.Domain;
using Microsoft.Extensions.Logging;

namespace EnergyManagement.Application.Analytics.Services.MachineLearning;

public class AnomalyDetectionService(ISensorReadingRepository sensorRepository, ILogger<AnomalyDetectionService> logger) : IAnomalyDetectionService
{
    public async Task<List<AnomalyDetectionDto>> DetectAnomaliesAsync(int userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var readings = await sensorRepository.GetReadingsByUserAndPeriodAsync(userId, startDate, endDate);
            var anomalies = new List<AnomalyDetectionDto>();

            if (!readings.Any())
                return anomalies;

            // Calcular estatísticas base
            var consumptionValues = readings.Select(r => r.Current * r.Voltage).ToList();
            var mean = consumptionValues.Average();
            var stdDev = CalculateStandardDeviation(consumptionValues, mean);

            // Threshold baseado em desvio padrão (Isolation Forest simplificado)
            var upperThreshold = mean + (2.5 * stdDev); // 2.5 sigma
            var lowerThreshold = Math.Max(0, mean - (2.5 * stdDev));

            foreach (var reading in readings)
            {
                var consumption = reading.Current * reading.Voltage;
                var anomalyScore = await CalculateAnomalyScoreAsync(consumption, consumptionValues);

                // Detectar anomalias de alto consumo
                if (consumption > upperThreshold)
                {
                    anomalies.Add(new AnomalyDetectionDto
                    {
                        DeviceId = reading.DeviceId,
                        AnomalyType = "High_Consumption",
                        Value = consumption,
                        ExpectedValue = mean,
                        AnomalyScore = anomalyScore,
                        Description = $"Consumo anormalmente alto detectado: {consumption:F2}W (esperado: ~{mean:F2}W)",
                        DetectedAt = reading.Timestamp,
                        Severity = anomalyScore > 0.8 ? "High" : anomalyScore > 0.6 ? "Medium" : "Low"
                    });
                }

                // Detectar anomalias de baixo consumo (possível falha)
                if (consumption < lowerThreshold && consumption > 0.1) // Evitar zeros normais
                {
                    anomalies.Add(new AnomalyDetectionDto
                    {
                        DeviceId = reading.DeviceId,
                        AnomalyType = "Low_Consumption",
                        Value = consumption,
                        ExpectedValue = mean,
                        AnomalyScore = anomalyScore,
                        Description = $"Consumo anormalmente baixo detectado: {consumption:F2}W (esperado: ~{mean:F2}W)",
                        DetectedAt = reading.Timestamp,
                        Severity = "Medium"
                    });
                }
            }

            // Detectar padrões anômalos (consumo durante horários incomuns)
            var nightReadings = readings.Where(r => r.Timestamp.Hour >= 0 && r.Timestamp.Hour < 6).ToList();
            var avgNightConsumption = nightReadings.Any() ? nightReadings.Average(r => r.Current * r.Voltage) : 0;
            var avgDayConsumption = readings.Where(r => r.Timestamp.Hour >= 6 && r.Timestamp.Hour < 22)
                .Average(r => r.Current * r.Voltage);

            if (avgNightConsumption > avgDayConsumption * 0.8) // Consumo noturno muito alto
            {
                anomalies.Add(new AnomalyDetectionDto
                {
                    AnomalyType = "Unusual_Pattern",
                    Value = avgNightConsumption,
                    ExpectedValue = avgDayConsumption * 0.3,
                    AnomalyScore = 0.7,
                    Description = "Padrão de consumo noturno anormalmente alto detectado",
                    DetectedAt = DateTime.UtcNow,
                    Severity = "Medium"
                });
            }

            logger.LogInformation("Detected {Count} anomalies for user {UserId}", anomalies.Count, userId);
            return anomalies.GroupBy(a => new { a.AnomalyType, Date = a.DetectedAt.Date })
                           .Select(g => g.First())
                           .ToList(); // Evitar duplicatas
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error detecting anomalies for user {UserId}", userId);
            return new List<AnomalyDetectionDto>();
        }
    }

    public async Task<double> CalculateAnomalyScoreAsync(double value, List<double> historicalValues)
    {
        if (!historicalValues.Any()) return 0;

        var mean = historicalValues.Average();
        var stdDev = CalculateStandardDeviation(historicalValues, mean);

        if (stdDev == 0) return 0;

        // Score baseado em quantos desvios padrão o valor está da média
        var zScore = Math.Abs((value - mean) / stdDev);

        // Normalizar para 0-1 (scores > 3 são considerados altamente anômalos)
        var anomalyScore = Math.Min(zScore / 3.0, 1.0);

        return await Task.FromResult(anomalyScore);
    }

    private double CalculateStandardDeviation(List<double> values, double mean)
    {
        if (values.Count <= 1) return 0;

        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }
}

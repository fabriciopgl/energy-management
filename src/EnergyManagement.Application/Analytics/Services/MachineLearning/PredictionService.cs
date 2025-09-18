using EnergyManagement.Application.Analytics.Models.Dtos;
using EnergyManagement.Application.Sensors.Domain;
using Microsoft.Extensions.Logging;

namespace EnergyManagement.Application.Analytics.Services.MachineLearning;

public class PredictionService(ISensorReadingRepository sensorRepository, ILogger<PredictionService> logger) : IPredictionService
{
    public async Task<List<HourlyConsumptionDto>> PredictHourlyConsumptionAsync(int userId, int hoursAhead = 24)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30); // Usar último mês para previsão

            var readings = await sensorRepository.GetReadingsByUserAndPeriodAsync(userId, startDate, endDate);

            if (!readings.Any())
                return new List<HourlyConsumptionDto>();

            // Agrupar por hora para identificar padrões
            var hourlyAverages = readings
                .GroupBy(r => r.Timestamp.Hour)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(r => r.Current * r.Voltage)
                );

            var predictions = new List<HourlyConsumptionDto>();
            var currentTime = DateTime.UtcNow;

            for (int i = 0; i < hoursAhead; i++)
            {
                var predictedHour = currentTime.AddHours(i);
                var hour = predictedHour.Hour;

                // Predição simples baseada em média histórica + variação sazonal
                var baseConsumption = hourlyAverages.ContainsKey(hour) ? hourlyAverages[hour] : 50; // Default 50W

                // Fatores de correção
                var weekdayFactor = predictedHour.DayOfWeek == DayOfWeek.Saturday || predictedHour.DayOfWeek == DayOfWeek.Sunday ? 1.1 : 1.0;
                var seasonalFactor = GetSeasonalFactor(predictedHour);

                var predictedConsumption = baseConsumption * weekdayFactor * seasonalFactor;

                predictions.Add(new HourlyConsumptionDto
                {
                    Hour = predictedHour,
                    Consumption = 0, // Valor real não disponível para futuro
                    PredictedConsumption = Math.Max(0, predictedConsumption)
                });
            }

            logger.LogInformation("Generated {Count} hourly predictions for user {UserId}", predictions.Count, userId);
            return predictions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error predicting hourly consumption for user {UserId}", userId);
            return new List<HourlyConsumptionDto>();
        }
    }

    public async Task<List<DailyConsumptionDto>> PredictDailyConsumptionAsync(int userId, int daysAhead = 7)
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-60); // Usar últimos 2 meses

            var readings = await sensorRepository.GetReadingsByUserAndPeriodAsync(userId, startDate, endDate);

            if (!readings.Any())
                return new List<DailyConsumptionDto>();

            // Calcular média diária histórica
            var dailyAverages = readings
                .GroupBy(r => r.Timestamp.Date)
                .Select(g => new { Date = g.Key, Consumption = g.Sum(r => r.Current * r.Voltage * 0.001) }) // Convert to kWh
                .ToList();

            var avgDailyConsumption = dailyAverages.Average(x => x.Consumption);
            var trend = CalculateTrend(dailyAverages.Select(x => x.Consumption).ToList());

            var predictions = new List<DailyConsumptionDto>();

            for (int i = 0; i < daysAhead; i++)
            {
                var predictedDate = endDate.AddDays(i + 1);

                // Aplicar tendência linear simples
                var trendAdjustment = trend * (i + 1);
                var baseConsumption = avgDailyConsumption + trendAdjustment;

                // Fatores de correção
                var weekdayFactor = GetWeekdayFactor(predictedDate.DayOfWeek);
                var seasonalFactor = GetSeasonalFactor(predictedDate);

                var predictedConsumption = baseConsumption * weekdayFactor * seasonalFactor;

                predictions.Add(new DailyConsumptionDto
                {
                    Date = predictedDate,
                    Consumption = 0, // Valor real não disponível
                    PredictedConsumption = Math.Max(0, predictedConsumption),
                    DayOfWeek = predictedDate.DayOfWeek.ToString()
                });
            }

            logger.LogInformation("Generated {Count} daily predictions for user {UserId}", predictions.Count, userId);
            return predictions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error predicting daily consumption for user {UserId}", userId);
            return new List<DailyConsumptionDto>();
        }
    }

    public async Task<double> PredictNextHourConsumptionAsync(int userId)
    {
        try
        {
            var hourlyPredictions = await PredictHourlyConsumptionAsync(userId, 1);
            return hourlyPredictions.FirstOrDefault()?.PredictedConsumption ?? 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error predicting next hour consumption for user {UserId}", userId);
            return 0;
        }
    }

    private double GetSeasonalFactor(DateTime date)
    {
        // Fator sazonal simples baseado no mês
        return date.Month switch
        {
            12 or 1 or 2 => 1.2, // Verão (Brasil) - maior uso de ar condicionado
            6 or 7 or 8 => 1.1,   // Inverno - aquecimento
            _ => 1.0
        };
    }

    private double GetWeekdayFactor(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Saturday or DayOfWeek.Sunday => 1.15, // Mais consumo em fins de semana
            _ => 1.0
        };
    }

    private double CalculateTrend(List<double> values)
    {
        if (values.Count < 2) return 0;

        // Regressão linear simples para calcular tendência
        var n = values.Count;
        var x = Enumerable.Range(0, n).Select(i => (double)i).ToList();
        var y = values;

        var sumX = x.Sum();
        var sumY = y.Sum();
        var sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
        var sumXX = x.Sum(xi => xi * xi);

        var slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
        return slope;
    }
}

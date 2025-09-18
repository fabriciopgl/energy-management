using EnergyManagement.Application.Analytics.Domain;
using EnergyManagement.Application.Analytics.Models.Dtos;
using EnergyManagement.Application.Analytics.Models.Requests;
using EnergyManagement.Application.Analytics.Services.MachineLearning;
using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Core.Common;
using Microsoft.Extensions.Logging;

namespace EnergyManagement.Application.Analytics.Services;

public class AnalyticsApplicationService : IAnalyticsApplicationService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ISensorReadingRepository _sensorRepository;
    private readonly IClusteringService _clusteringService;
    private readonly IAnomalyDetectionService _anomalyService;
    private readonly IPredictionService _predictionService;
    private readonly ILogger<AnalyticsApplicationService> _logger;

    public AnalyticsApplicationService(
        IAnalyticsRepository analyticsRepository,
        ISensorReadingRepository sensorRepository,
        IClusteringService clusteringService,
        IAnomalyDetectionService anomalyService,
        IPredictionService predictionService,
        ILogger<AnalyticsApplicationService> logger)
    {
        _analyticsRepository = analyticsRepository;
        _sensorRepository = sensorRepository;
        _clusteringService = clusteringService;
        _anomalyService = anomalyService;
        _predictionService = predictionService;
        _logger = logger;
    }

    public async Task<Result<EnergyAnalysisDto>> AnalyzeConsumptionAsync(int userId, AnalyzeConsumptionRequest request)
    {
        try
        {
            var endDate = request.EndDate ?? DateTime.UtcNow;
            var startDate = request.StartDate ?? endDate.AddDays(-30);

            var readings = await _sensorRepository.GetReadingsByUserAndPeriodAsync(userId, startDate, endDate);

            if (!readings.Any())
            {
                return Result<EnergyAnalysisDto>.Success(new EnergyAnalysisDto
                {
                    AnalysisDate = DateTime.UtcNow,
                    TotalConsumption = 0,
                    AverageDaily = 0,
                    AverageHourly = 0
                });
            }

            // Calcular métricas básicas
            var totalConsumption = readings.Sum(r => r.Current * r.Voltage * 0.001); // kWh
            var daysDiff = Math.Max(1, (endDate - startDate).Days);
            var averageDaily = totalConsumption / daysDiff;
            var averageHourly = totalConsumption / (daysDiff * 24);

            var analysis = new EnergyAnalysisDto
            {
                AnalysisDate = DateTime.UtcNow,
                TotalConsumption = totalConsumption,
                AverageDaily = averageDaily,
                AverageHourly = averageHourly
            };

            // Adicionar padrões se solicitado
            if (request.IncludeAnomalies)
            {
                analysis.Patterns = await _clusteringService.IdentifyConsumptionPatternsAsync(userId, startDate, endDate);
            }

            // Adicionar anomalias se solicitado
            if (request.IncludeAnomalies)
            {
                analysis.Anomalies = await _anomalyService.DetectAnomaliesAsync(userId, startDate, endDate);
            }

            // Gerar recomendações se solicitado
            if (request.IncludeRecommendations)
            {
                analysis.Recommendations = await GenerateRecommendationsAsync(userId, analysis.Patterns, analysis.Anomalies, averageDaily);
            }

            // Adicionar predições se solicitado
            if (request.IncludePredictions)
            {
                var hourlyPredictions = await _predictionService.PredictHourlyConsumptionAsync(userId, 24);
                var dailyPredictions = await _predictionService.PredictDailyConsumptionAsync(userId, 7);

                analysis.Trend = new ConsumptionTrendDto
                {
                    TrendDirection = CalculateTrendDirection(readings),
                    TrendPercentage = CalculateTrendPercentage(readings),
                    Period = $"{startDate:dd/MM} - {endDate:dd/MM}",
                    HourlyData = hourlyPredictions,
                    DailyData = dailyPredictions
                };
            }

            _logger.LogInformation("Analysis completed for user {UserId}: {TotalConsumption} kWh", userId, totalConsumption);
            return Result<EnergyAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing consumption for user {UserId}", userId);
            return Result<EnergyAnalysisDto>.Failure("Erro ao realizar análise de consumo");
        }
    }

    public async Task<Result<List<ConsumptionPatternDto>>> GetConsumptionPatternsAsync(int userId)
    {
        try
        {
            var patterns = await _analyticsRepository.GetUserPatternsAsync(userId);
            var dto = patterns.Select(p => new ConsumptionPatternDto
            {
                Id = p.Id,
                PatternType = p.PatternType,
                AverageConsumption = p.AverageConsumption,
                PeakConsumption = p.PeakConsumption,
                StartTime = p.StartTime,
                EndTime = p.EndTime,
                ClusterId = p.ClusterId,
                AnalyzedAt = p.AnalyzedAt,
                DeviceNames = string.IsNullOrEmpty(p.DeviceNames) ? new List<string>() :
                            System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.DeviceNames) ?? new List<string>()
            }).ToList();

            return Result<List<ConsumptionPatternDto>>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consumption patterns for user {UserId}", userId);
            return Result<List<ConsumptionPatternDto>>.Failure("Erro ao obter padrões de consumo");
        }
    }

    public async Task<Result<List<AnomalyDetectionDto>>> GetAnomaliesAsync(int userId, bool includeResolved = false)
    {
        try
        {
            var anomalies = await _analyticsRepository.GetUserAnomaliesAsync(userId, includeResolved);
            var dto = anomalies.Select(a => new AnomalyDetectionDto
            {
                Id = a.Id,
                DeviceId = a.DeviceId,
                AnomalyType = a.AnomalyType,
                Value = a.Value,
                ExpectedValue = a.ExpectedValue,
                AnomalyScore = a.AnomalyScore,
                Description = a.Description,
                DetectedAt = a.DetectedAt,
                IsResolved = a.IsResolved,
                ResolvedAt = a.ResolvedAt,
                Severity = GetSeverityFromScore(a.AnomalyScore)
            }).ToList();

            return Result<List<AnomalyDetectionDto>>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting anomalies for user {UserId}", userId);
            return Result<List<AnomalyDetectionDto>>.Failure("Erro ao obter anomalias");
        }
    }

    public async Task<Result<List<EnergyRecommendationDto>>> GetRecommendationsAsync(int userId, bool includeApplied = false)
    {
        try
        {
            var recommendations = await _analyticsRepository.GetUserRecommendationsAsync(userId, includeApplied);
            var dto = recommendations.Select(r => new EnergyRecommendationDto
            {
                Id = r.Id,
                RecommendationType = r.RecommendationType,
                Title = r.Title,
                Description = r.Description,
                PotentialSavings = r.PotentialSavings,
                PotentialSavingsPercent = r.PotentialSavingsPercent,
                CreatedAt = r.CreatedAt,
                IsApplied = r.IsApplied,
                AppliedAt = r.AppliedAt,
                Priority = GetPriorityFromSavings(r.PotentialSavingsPercent)
            }).ToList();

            return Result<List<EnergyRecommendationDto>>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for user {UserId}", userId);
            return Result<List<EnergyRecommendationDto>>.Failure("Erro ao obter recomendações");
        }
    }

    public async Task<Result> ResolveAnomalyAsync(int userId, ResolveAnomalyRequest request)
    {
        try
        {
            await _analyticsRepository.MarkAnomalyAsResolvedAsync(request.AnomalyId);
            _logger.LogInformation("Anomaly {AnomalyId} resolved by user {UserId}", request.AnomalyId, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving anomaly {AnomalyId} for user {UserId}", request.AnomalyId, userId);
            return Result.Failure("Erro ao resolver anomalia");
        }
    }

    public async Task<Result> ApplyRecommendationAsync(int userId, int recommendationId)
    {
        try
        {
            await _analyticsRepository.MarkRecommendationAsAppliedAsync(recommendationId);
            _logger.LogInformation("Recommendation {RecommendationId} applied by user {UserId}", recommendationId, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying recommendation {RecommendationId} for user {UserId}", recommendationId, userId);
            return Result.Failure("Erro ao aplicar recomendação");
        }
    }

    public async Task<Result> RunAnalysisAsync(int userId)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);

            // Executar análise de padrões
            var patterns = await _clusteringService.IdentifyConsumptionPatternsAsync(userId, startDate, endDate);
            foreach (var pattern in patterns)
            {
                var entity = new ConsumptionPattern
                {
                    UserId = userId,
                    PatternType = pattern.PatternType,
                    AverageConsumption = pattern.AverageConsumption,
                    PeakConsumption = pattern.PeakConsumption,
                    StartTime = pattern.StartTime,
                    EndTime = pattern.EndTime,
                    ClusterId = pattern.ClusterId,
                    AnalyzedAt = DateTime.UtcNow,
                    DeviceNames = System.Text.Json.JsonSerializer.Serialize(pattern.DeviceNames)
                };
                await _analyticsRepository.SavePatternAsync(entity);
            }

            // Executar detecção de anomalias
            var anomalies = await _anomalyService.DetectAnomaliesAsync(userId, startDate, endDate);
            foreach (var anomaly in anomalies)
            {
                var entity = new AnomalyDetection
                {
                    UserId = userId,
                    DeviceId = anomaly.DeviceId,
                    AnomalyType = anomaly.AnomalyType,
                    Value = anomaly.Value,
                    ExpectedValue = anomaly.ExpectedValue,
                    AnomalyScore = anomaly.AnomalyScore,
                    Description = anomaly.Description,
                    DetectedAt = anomaly.DetectedAt,
                    IsResolved = false
                };
                await _analyticsRepository.SaveAnomalyAsync(entity);
            }

            _logger.LogInformation("Analysis completed for user {UserId}: {PatternCount} patterns, {AnomalyCount} anomalies",
                userId, patterns.Count, anomalies.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running analysis for user {UserId}", userId);
            return Result.Failure("Erro ao executar análise");
        }
    }

    private async Task<List<EnergyRecommendationDto>> GenerateRecommendationsAsync(int userId, List<ConsumptionPatternDto> patterns, List<AnomalyDetectionDto> anomalies, double averageDaily)
    {
        var recommendations = new List<EnergyRecommendationDto>();

        // Recomendação baseada em padrões de pico
        var eveningPattern = patterns.FirstOrDefault(p => p.PatternType == "Evening");
        if (eveningPattern?.AverageConsumption > averageDaily * 0.4) // Mais de 40% do consumo à noite
        {
            recommendations.Add(new EnergyRecommendationDto
            {
                RecommendationType = "Time_Shift",
                Title = "Desloque o uso de equipamentos para fora do horário de pico",
                Description = "Seu maior consumo está entre 18h-24h. Considere usar máquina de lavar e outros equipamentos durante a tarde.",
                PotentialSavings = eveningPattern.AverageConsumption * 0.2,
                PotentialSavingsPercent = 20,
                CreatedAt = DateTime.UtcNow,
                Priority = "High"
            });
        }

        // Recomendação baseada em anomalias
        if (anomalies.Any(a => a.AnomalyType == "High_Consumption"))
        {
            recommendations.Add(new EnergyRecommendationDto
            {
                RecommendationType = "Usage_Reduction",
                Title = "Verifique equipamentos com consumo elevado",
                Description = "Detectamos picos de consumo anômalos. Verifique se há equipamentos defeituosos ou em standby desnecessariamente.",
                PotentialSavings = averageDaily * 0.15,
                PotentialSavingsPercent = 15,
                CreatedAt = DateTime.UtcNow,
                Priority = "Medium"
            });
        }

        // Recomendação de eficiência geral
        if (averageDaily > 10) // Se consume mais que 10 kWh/dia
        {
            recommendations.Add(new EnergyRecommendationDto
            {
                RecommendationType = "Device_Optimization",
                Title = "Considere equipamentos mais eficientes",
                Description = "Seu consumo está acima da média. Lâmpadas LED e eletrodomésticos com selo A de eficiência podem reduzir significativamente o consumo.",
                PotentialSavings = averageDaily * 0.25,
                PotentialSavingsPercent = 25,
                CreatedAt = DateTime.UtcNow,
                Priority = "Medium"
            });
        }

        return recommendations;
    }

    private string CalculateTrendDirection(IReadOnlyList<dynamic> readings)
    {
        if (readings.Count < 7) return "Stable";

        // Simplificado: comparar primeira e última semana
        var firstWeek = readings.Take(readings.Count / 2).ToList();
        var lastWeek = readings.Skip(readings.Count / 2).ToList();

        // Esta é uma simplificação - em produção usaria cálculo de tendência mais robusto
        return "Stable";
    }

    private double CalculateTrendPercentage(IReadOnlyList<dynamic> readings)
    {
        // Simplificado - retorna 0 por enquanto
        return 0;
    }

    private string GetSeverityFromScore(double score)
    {
        return score switch
        {
            > 0.8 => "High",
            > 0.6 => "Medium",
            _ => "Low"
        };
    }

    private string GetPriorityFromSavings(double savingsPercent)
    {
        return savingsPercent switch
        {
            > 20 => "High",
            > 10 => "Medium",
            _ => "Low"
        };
    }
}

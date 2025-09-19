using EnergyManagement.Application.Devices.Domain;
using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Core.Common;
using Microsoft.Extensions.Logging;

namespace EnergyManagement.Application.Analytics.Services;
public class EnergyAnalysisApplicationService : IEnergyAnalysisApplicationService
{
    private readonly ISensorReadingRepository _sensorRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<EnergyAnalysisApplicationService> _logger;

    public EnergyAnalysisApplicationService(
        ISensorReadingRepository sensorRepository,
        IDeviceRepository deviceRepository,
        ILogger<EnergyAnalysisApplicationService> logger)
    {
        _sensorRepository = sensorRepository;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    public async Task<Result<EnergyDashboardDto>> GetDashboardAnalyticsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var endDateTime = endDate ?? DateTime.UtcNow;
            var startDateTime = startDate ?? endDateTime.AddDays(-30);

            // Obter dispositivos do usuário
            var userDevices = await _deviceRepository.GetByUserIdAsync(userId);
            if (!userDevices.Any())
            {
                return Result<EnergyDashboardDto>.Success(new EnergyDashboardDto
                {
                    TotalConsumption = 0,
                    AverageDaily = 0,
                    EstimatedMonthlyCost = 0,
                    DevicesCount = 0,
                    LastUpdated = DateTime.UtcNow,
                    PeakHour = "Sem dados",
                    EfficiencyScore = 0,
                    ComparedToLastMonth = 0
                });
            }

            var deviceIds = userDevices.Select(d => d.Id).ToList();

            // Obter leituras reais do período
            var readings = await _sensorRepository.GetByDevicesAndDateRangeAsync(deviceIds, startDateTime, endDateTime);

            if (!readings.Any())
            {
                return Result<EnergyDashboardDto>.Success(new EnergyDashboardDto
                {
                    TotalConsumption = 0,
                    AverageDaily = 0,
                    EstimatedMonthlyCost = 0,
                    DevicesCount = userDevices.Count,
                    LastUpdated = DateTime.UtcNow,
                    PeakHour = "Sem dados",
                    EfficiencyScore = 0,
                    ComparedToLastMonth = 0
                });
            }

            // Cálculos básicos com dados reais
            var totalConsumption = readings.Where(r => r.Energy > 0.08).Sum(r => r.Energy);
            var totalDays = (endDateTime - startDateTime).TotalDays;
            var averageDaily = totalDays > 0 ? totalConsumption / totalDays : 0;
            var estimatedMonthlyCost = decimal.Parse(averageDaily.ToString()) * 30 * 0.75m; // R$ 0,75 por kWh (tarifa média)

            // Análise de horário de pico com dados reais
            var hourlyConsumption = readings
                .GroupBy(r => r.Timestamp.Hour)
                .Select(g => new { Hour = g.Key, Consumption = g.Sum(r => r.Power) })
                .OrderByDescending(h => h.Consumption)
                .FirstOrDefault();

            var peakHour = hourlyConsumption != null ? $"{hourlyConsumption.Hour:00}:00" : "Sem dados";

            // Score de eficiência baseado nos dados reais
            var efficiencyScore = CalculateEfficiencyScore(readings);

            // Comparação com mês anterior com dados reais
            var comparedToLastMonth = await CalculateMonthlyComparisonAsync(deviceIds, startDateTime);

            // Obter dados de consumo por hora e por dia
            var hourlyData = await GetRealHourlyConsumptionDataAsync(deviceIds, startDateTime, endDateTime);
            var dailyData = await GetRealDailyConsumptionDataAsync(deviceIds, startDateTime, endDateTime);

            var dashboard = new EnergyDashboardDto
            {
                TotalConsumption = Math.Round(totalConsumption, 2),
                AverageDaily = Math.Round(averageDaily, 2),
                EstimatedMonthlyCost = Math.Round(estimatedMonthlyCost, 2),
                DevicesCount = userDevices.Count,
                LastUpdated = readings.Any() ? readings.Max(r => r.Timestamp) : DateTime.UtcNow,
                PeakHour = peakHour,
                EfficiencyScore = efficiencyScore,
                ComparedToLastMonth = comparedToLastMonth,
                HourlyConsumption = hourlyData,
                DailyConsumption = dailyData
            };

            return Result<EnergyDashboardDto>.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter analytics do dashboard para usuário {UserId}", userId);
            return Result<EnergyDashboardDto>.Failure("Erro interno ao processar análise energética");
        }
    }

    public async Task<Result<IReadOnlyList<EnergyPatternDto>>> GetConsumptionPatternsAsync(int userId)
    {
        try
        {
            var userDevices = await _deviceRepository.GetByUserIdAsync(userId);
            var deviceIds = userDevices.Select(d => d.Id).ToList();

            if (!deviceIds.Any())
            {
                return Result<IReadOnlyList<EnergyPatternDto>>.Success(new List<EnergyPatternDto>());
            }

            // Obter leituras dos últimos 30 dias para análise de padrões
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);
            var readings = await _sensorRepository.GetByDevicesAndDateRangeAsync(deviceIds, startDate, endDate);

            if (!readings.Any())
            {
                return Result<IReadOnlyList<EnergyPatternDto>>.Success(new List<EnergyPatternDto>());
            }

            var patterns = new List<EnergyPatternDto>();

            // Padrão Matinal (6h-10h)
            var morningReadings = readings.Where(r => r.Timestamp.Hour >= 6 && r.Timestamp.Hour <= 10).ToList();
            if (morningReadings.Any())
            {
                patterns.Add(new EnergyPatternDto
                {
                    PatternName = "Consumo Matinal",
                    Description = $"Alto consumo entre 6h-10h ({morningReadings.Count} leituras)",
                    AverageConsumption = Math.Round(morningReadings.Average(r => r.Power), 2),
                    Frequency = "Diário",
                    EfficiencyRating = GetEfficiencyRating(morningReadings.Average(r => r.Power))
                });
            }

            // Padrão Noturno (18h-22h)
            var eveningReadings = readings.Where(r => r.Timestamp.Hour >= 18 && r.Timestamp.Hour <= 22).ToList();
            if (eveningReadings.Any())
            {
                patterns.Add(new EnergyPatternDto
                {
                    PatternName = "Consumo Noturno",
                    Description = $"Consumo elevado entre 18h-22h ({eveningReadings.Count} leituras)",
                    AverageConsumption = Math.Round(eveningReadings.Average(r => r.Power), 2),
                    Frequency = "Diário",
                    EfficiencyRating = GetEfficiencyRating(eveningReadings.Average(r => r.Power))
                });
            }

            // Padrão de Madrugada (0h-6h)
            var nightReadings = readings.Where(r => r.Timestamp.Hour >= 0 && r.Timestamp.Hour <= 6).ToList();
            if (nightReadings.Any())
            {
                patterns.Add(new EnergyPatternDto
                {
                    PatternName = "Consumo de Madrugada",
                    Description = $"Consumo base entre 0h-6h ({nightReadings.Count} leituras)",
                    AverageConsumption = Math.Round(nightReadings.Average(r => r.Power), 2),
                    Frequency = "Contínuo",
                    EfficiencyRating = GetEfficiencyRating(nightReadings.Average(r => r.Power))
                });
            }

            // Padrão de Fim de Semana vs Dias Úteis
            var weekdayReadings = readings.Where(r => r.Timestamp.DayOfWeek >= DayOfWeek.Monday && r.Timestamp.DayOfWeek <= DayOfWeek.Friday).ToList();
            var weekendReadings = readings.Where(r => r.Timestamp.DayOfWeek == DayOfWeek.Saturday || r.Timestamp.DayOfWeek == DayOfWeek.Sunday).ToList();

            if (weekdayReadings.Any() && weekendReadings.Any())
            {
                var weekdayAvg = weekdayReadings.Average(r => r.Power);
                var weekendAvg = weekendReadings.Average(r => r.Power);
                var difference = Math.Abs(weekdayAvg - weekendAvg);

                if (difference > weekdayAvg * 0.1) // Diferença significativa (>10%)
                {
                    patterns.Add(new EnergyPatternDto
                    {
                        PatternName = weekendAvg > weekdayAvg ? "Maior Consumo nos Fins de Semana" : "Maior Consumo em Dias Úteis",
                        Description = $"Diferença de {difference:F1}W entre padrões semanais",
                        AverageConsumption = Math.Round(Math.Max(weekdayAvg, weekendAvg), 2),
                        Frequency = weekendAvg > weekdayAvg ? "Fins de Semana" : "Dias Úteis",
                        EfficiencyRating = GetEfficiencyRating(Math.Max(weekdayAvg, weekendAvg))
                    });
                }
            }

            return Result<IReadOnlyList<EnergyPatternDto>>.Success(patterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter padrões de consumo para usuário {UserId}", userId);
            return Result<IReadOnlyList<EnergyPatternDto>>.Failure("Erro ao analisar padrões de consumo");
        }
    }

    public async Task<Result<IReadOnlyList<AnomalyDetectionDto>>> DetectAnomaliesAsync(int userId, int days = 30)
    {
        try
        {
            var userDevices = await _deviceRepository.GetByUserIdAsync(userId);
            var deviceIds = userDevices.Select(d => d.Id).ToList();

            if (!deviceIds.Any())
            {
                return Result<IReadOnlyList<AnomalyDetectionDto>>.Success(new List<AnomalyDetectionDto>());
            }

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);
            var readings = await _sensorRepository.GetByDevicesAndDateRangeAsync(deviceIds, startDate, endDate);

            var anomalies = new List<AnomalyDetectionDto>();

            if (!readings.Any())
            {
                return Result<IReadOnlyList<AnomalyDetectionDto>>.Success(anomalies);
            }

            // Agrupar por dispositivo para análise individual
            var readingsByDevice = readings.GroupBy(r => r.DeviceId);

            foreach (var deviceReadings in readingsByDevice)
            {
                var deviceId = deviceReadings.Key;
                var device = userDevices.FirstOrDefault(d => d.Id == deviceId);
                var deviceName = device?.Name ?? "Dispositivo Desconhecido";

                var deviceData = deviceReadings.OrderBy(r => r.Timestamp).ToList();

                if (deviceData.Count < 10) // Dados insuficientes para análise
                    continue;

                // Calcular estatísticas para detecção de anomalias
                var powerValues = deviceData.Select(r => r.Power).ToList();
                var avgPower = powerValues.Average();
                var stdDeviation = CalculateStandardDeviation(powerValues);
                var upperThreshold = avgPower + (2.5 * stdDeviation); // 2.5 desvios padrão
                var lowerThreshold = Math.Max(0, avgPower - (2.5 * stdDeviation));

                // Detectar anomalias de consumo excessivo
                var highPowerAnomalies = deviceData.Where(r => r.Power > upperThreshold).ToList();
                foreach (var anomaly in highPowerAnomalies)
                {
                    var percentageAbove = ((anomaly.Power - avgPower) / avgPower) * 100;
                    anomalies.Add(new AnomalyDetectionDto
                    {
                        DeviceName = deviceName,
                        Timestamp = anomaly.Timestamp,
                        PowerValue = anomaly.Power,
                        ExpectedRange = $"{avgPower:F2} ± {stdDeviation:F2} W",
                        AnomalyType = "Consumo Excessivo",
                        Severity = anomaly.Power > upperThreshold * 1.5 ? "Alta" : "Média",
                        Description = $"Consumo de {anomaly.Power:F2}W detectado, {percentageAbove:F1}% acima do normal"
                    });
                }

                // Detectar anomalias de consumo muito baixo (possível falha)
                var lowPowerAnomalies = deviceData.Where(r => r.Power < lowerThreshold && r.Power > 0).ToList();
                foreach (var anomaly in lowPowerAnomalies)
                {
                    var percentageBelow = ((avgPower - anomaly.Power) / avgPower) * 100;
                    anomalies.Add(new AnomalyDetectionDto
                    {
                        DeviceName = deviceName,
                        Timestamp = anomaly.Timestamp,
                        PowerValue = anomaly.Power,
                        ExpectedRange = $"{avgPower:F2} ± {stdDeviation:F2} W",
                        AnomalyType = "Consumo Anormalmente Baixo",
                        Severity = "Baixa",
                        Description = $"Consumo de {anomaly.Power:F2}W detectado, {percentageBelow:F1}% abaixo do normal"
                    });
                }

                // Detectar possíveis falhas (leituras zeradas por muito tempo)
                var zeroReadings = deviceData.Where(r => r.Power == 0).ToList();
                if (zeroReadings.Count > deviceData.Count * 0.1) // Mais de 10% das leituras zeradas
                {
                    var firstZero = zeroReadings.First();
                    anomalies.Add(new AnomalyDetectionDto
                    {
                        DeviceName = deviceName,
                        Timestamp = firstZero.Timestamp,
                        PowerValue = 0,
                        ExpectedRange = $"> 0 W",
                        AnomalyType = "Possível Falha do Sensor",
                        Severity = "Alta",
                        Description = $"Múltiplas leituras zeradas detectadas ({zeroReadings.Count} de {deviceData.Count})"
                    });
                }

                // Detectar variações bruscas de tensão
                var voltageAnomalies = deviceData.Where(r => r.Voltage < 200 || r.Voltage > 240).ToList();
                foreach (var anomaly in voltageAnomalies)
                {
                    anomalies.Add(new AnomalyDetectionDto
                    {
                        DeviceName = deviceName,
                        Timestamp = anomaly.Timestamp,
                        PowerValue = anomaly.Power,
                        ExpectedRange = "200-240 V",
                        AnomalyType = "Tensão Fora do Padrão",
                        Severity = Math.Abs(anomaly.Voltage - 220) > 30 ? "Alta" : "Média",
                        Description = $"Tensão de {anomaly.Voltage:F1}V detectada (esperado: ~220V)"
                    });
                }
            }

            // Ordenar por severidade e timestamp
            var sortedAnomalies = anomalies
                .OrderByDescending(a => a.Severity == "Alta" ? 3 : a.Severity == "Média" ? 2 : 1)
                .ThenByDescending(a => a.Timestamp)
                .Take(50) // Limitar a 50 anomalias mais relevantes
                .ToList();

            return Result<IReadOnlyList<AnomalyDetectionDto>>.Success(sortedAnomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao detectar anomalias para usuário {UserId}", userId);
            return Result<IReadOnlyList<AnomalyDetectionDto>>.Failure("Erro ao detectar anomalias");
        }
    }

    public async Task<Result<EnergyForecastDto>> GetEnergyForecastAsync(int userId, int days = 7)
    {
        try
        {
            var userDevices = await _deviceRepository.GetByUserIdAsync(userId);
            var deviceIds = userDevices.Select(d => d.Id).ToList();

            if (!deviceIds.Any())
            {
                return Result<EnergyForecastDto>.Success(new EnergyForecastDto
                {
                    ForecastDays = days,
                    GeneratedAt = DateTime.UtcNow,
                    Confidence = 0,
                    DailyForecasts = new List<DailyForecastDto>()
                });
            }

            // Obter dados históricos dos últimos 30 dias para fazer previsão
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);
            var historicalReadings = await _sensorRepository.GetByDevicesAndDateRangeAsync(deviceIds, startDate, endDate);

            var forecast = new EnergyForecastDto
            {
                ForecastDays = days,
                GeneratedAt = DateTime.UtcNow,
                DailyForecasts = new List<DailyForecastDto>()
            };

            if (!historicalReadings.Any())
            {
                forecast.Confidence = 0;
                return Result<EnergyForecastDto>.Success(forecast);
            }

            // Agrupar por dia para calcular consumo diário
            var dailyConsumption = historicalReadings
                .GroupBy(r => r.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalEnergy = g.Where(r => r.Energy > 0.08).Sum(r => r.Energy),
                    AveragePower = g.Average(r => r.Power),
                    ReadingsCount = g.Count()
                })
                .Where(d => d.ReadingsCount >= 10) // Apenas dias com dados suficientes
                .OrderBy(d => d.Date)
                .ToList();

            if (dailyConsumption.Count < 7) // Dados insuficientes para previsão confiável
            {
                forecast.Confidence = 0.3m;
            }
            else
            {
                forecast.Confidence = Math.Min(0.95m, 0.5m + (dailyConsumption.Count * 0.015m));
            }

            // Calcular médias baseadas em dados reais
            var avgDailyConsumption = dailyConsumption.Any() ? dailyConsumption.Average(d => d.TotalEnergy) : 0;
            var trend = dailyConsumption.Count >= 7 ? CalculateLinearTrend(dailyConsumption.Select(d => d.TotalEnergy).ToList()) : 0;

            // Calcular padrões por dia da semana
            var weekdayPatterns = historicalReadings
                .GroupBy(r => r.Timestamp.DayOfWeek)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(r => r.Timestamp.Date).Average(dayGroup => dayGroup.Where(r => r.Energy > 0.08).Sum(r => r.Energy))
                );

            // Gerar previsões baseadas nos dados reais
            for (int i = 1; i <= days; i++)
            {
                var forecastDate = DateTime.UtcNow.Date.AddDays(i);
                var dayOfWeek = forecastDate.DayOfWeek;

                // Usar padrão do dia da semana se disponível, senão usar média geral
                var basePrediction = weekdayPatterns.ContainsKey(dayOfWeek)
                    ? weekdayPatterns[dayOfWeek]
                    : avgDailyConsumption;

                // Aplicar tendência temporal
                var predictedConsumption = basePrediction + (trend * i);

                // Adicionar fator sazonal baseado em dados históricos
                var seasonalFactor = GetSeasonalFactorFromData(forecastDate, historicalReadings);
                predictedConsumption *= seasonalFactor;

                // Garantir que a previsão seja positiva
                predictedConsumption = Math.Max(0, predictedConsumption);

                // Calcular intervalo de confiança baseado na variabilidade dos dados
                var standardDeviation = dailyConsumption.Any()
                    ? CalculateStandardDeviation(dailyConsumption.Select(d => d.TotalEnergy))
                    : 0;
                var confidenceInterval = standardDeviation * 1.96; // 95% de confiança

                forecast.DailyForecasts.Add(new DailyForecastDto
                {
                    Date = forecastDate,
                    PredictedConsumption = Math.Round(predictedConsumption, 2),
                    EstimatedCost = Math.Round(decimal.Parse(predictedConsumption.ToString()) * 0.75m, 2),
                    ConfidenceInterval = $"±{Math.Round(confidenceInterval, 2)} kWh"
                });
            }

            forecast.TotalPredictedConsumption = forecast.DailyForecasts.Sum(f => f.PredictedConsumption);
            forecast.EstimatedTotalCost = forecast.DailyForecasts.Sum(f => f.EstimatedCost);

            return Result<EnergyForecastDto>.Success(forecast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar previsão energética para usuário {UserId}", userId);
            return Result<EnergyForecastDto>.Failure("Erro ao gerar previsão energética");
        }
    }

    public async Task<Result<IReadOnlyList<DeviceRankingDto>>> GetDeviceRankingAsync(int userId)
    {
        try
        {
            var userDevices = await _deviceRepository.GetByUserIdAsync(userId);
            var deviceIds = userDevices.Select(d => d.Id).ToList();

            if (!deviceIds.Any())
            {
                return Result<IReadOnlyList<DeviceRankingDto>>.Success(new List<DeviceRankingDto>());
            }

            // Obter leituras dos últimos 30 dias
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);
            var readings = await _sensorRepository.GetByDevicesAndDateRangeAsync(deviceIds, startDate, endDate);

            var deviceRanking = new List<DeviceRankingDto>();

            foreach (var device in userDevices)
            {
                var deviceReadings = readings.Where(r => r.DeviceId == device.Id).ToList();

                var totalConsumption = deviceReadings.Where(r => r.Energy > 0.08).Sum(r => r.Energy);
                var avgPower = deviceReadings.Any() ? deviceReadings.Average(r => r.Power) : 0;
                var maxPower = deviceReadings.Any() ? deviceReadings.Max(r => r.Power) : 0;
                var minPower = deviceReadings.Any() ? deviceReadings.Min(r => r.Power) : 0;

                // Calcular horas de operação baseado no número de leituras com potência > 0
                var activeReadings = deviceReadings.Where(r => r.Power > 0).Count();
                var operatingHours = activeReadings * 0.25; // Assumindo leituras a cada 15 min

                // Calcular eficiência baseada na consistência e variabilidade
                var efficiencyRating = CalculateDeviceEfficiency(deviceReadings);

                deviceRanking.Add(new DeviceRankingDto
                {
                    DeviceId = device.Id,
                    DeviceName = device.Name,
                    Location = device.Location ?? "Não informado",
                    TotalConsumption = Math.Round(totalConsumption, 2),
                    AveragePower = Math.Round(avgPower, 2),
                    EstimatedMonthlyCost = Math.Round(decimal.Parse(totalConsumption.ToString()) * 0.75m, 2),
                    OperatingHours = Math.Round(operatingHours, 1),
                    EfficiencyRating = efficiencyRating,
                    LastActivity = deviceReadings.Any() ? deviceReadings.Max(r => r.Timestamp) : device.CreatedAt
                });
            }

            // Ordenar por consumo total (maior para menor)
            var sortedRanking = deviceRanking.OrderByDescending(d => d.TotalConsumption).ToList();

            return Result<IReadOnlyList<DeviceRankingDto>>.Success(sortedRanking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter ranking de dispositivos para usuário {UserId}", userId);
            return Result<IReadOnlyList<DeviceRankingDto>>.Failure("Erro ao gerar ranking de dispositivos");
        }
    }

    // Métodos auxiliares atualizados
    private async Task<double> CalculateMonthlyComparisonAsync(IEnumerable<int> deviceIds, DateTime currentPeriodStart)
    {
        try
        {
            // Período atual (últimos 30 dias)
            var currentPeriodEnd = DateTime.UtcNow;
            var currentReadings = await _sensorRepository.GetByDevicesAndDateRangeAsync(deviceIds, currentPeriodStart, currentPeriodEnd);

            // Período anterior (30 dias antes do período atual)
            var previousPeriodEnd = currentPeriodStart;
            var previousPeriodStart = previousPeriodEnd.AddDays(-30);
            var previousReadings = await _sensorRepository.GetByDevicesAndDateRangeAsync(deviceIds, previousPeriodStart, previousPeriodEnd);

            if (!previousReadings.Any())
                return 0; // Sem dados para comparação

            var currentConsumption = currentReadings.Where(r => r.Energy > 0.08).Sum(r => r.Energy);
            var previousConsumption = previousReadings.Where(r => r.Energy > 0.08).Sum(r => r.Energy);

            if (previousConsumption == 0)
                return currentConsumption > 0 ? 100 : 0;

            var percentageChange = ((currentConsumption - previousConsumption) / previousConsumption) * 100;
            return Math.Round(percentageChange, 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular comparação mensal");
            return 0;
        }
    }

    private async Task<List<HourlyConsumptionDto>> GetRealHourlyConsumptionDataAsync(IEnumerable<int> deviceIds, DateTime startDate, DateTime endDate)
    {
        try
        {
            var hourlyData = await _sensorRepository.GetHourlyAggregationsAsync(deviceIds, startDate, endDate);

            return hourlyData.Select(h => new HourlyConsumptionDto
            {
                Hour = h.Hour,
                Consumption = Math.Round(h.TotalEnergy, 2)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter dados de consumo por hora");
            return Enumerable.Range(0, 24).Select(hour => new HourlyConsumptionDto
            {
                Hour = hour,
                Consumption = 0
            }).ToList();
        }
    }

    private async Task<List<DailyConsumptionDto>> GetRealDailyConsumptionDataAsync(IEnumerable<int> deviceIds, DateTime startDate, DateTime endDate)
    {
        try
        {
            var dailyData = await _sensorRepository.GetDailyAggregationsAsync(deviceIds, startDate, endDate);

            return dailyData.Select(d => new DailyConsumptionDto
            {
                Date = d.Date,
                Consumption = Math.Round(d.TotalEnergy, 2)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter dados de consumo diário");

            // Retornar dados vazios para o período se houver erro
            var result = new List<DailyConsumptionDto>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                result.Add(new DailyConsumptionDto
                {
                    Date = date,
                    Consumption = 0
                });
            }
            return result;
        }
    }

    private static string CalculateDeviceEfficiency(IReadOnlyList<SensorReading> readings)
    {
        if (!readings.Any())
            return "Sem Dados";

        // Calcular eficiência baseada na estabilidade do consumo
        var powerValues = readings.Select(r => r.Power).ToList();
        var avgPower = powerValues.Average();
        var stdDeviation = CalculateStandardDeviation(powerValues);
        var coefficientOfVariation = avgPower > 0 ? stdDeviation / avgPower : 0;

        // Classificar eficiência baseada na variabilidade
        return coefficientOfVariation switch
        {
            < 0.2 => "Excelente", // Muito estável
            < 0.4 => "Bom",       // Razoavelmente estável  
            < 0.6 => "Regular",   // Moderadamente variável
            _ => "Precisa Melhorar" // Muito variável
        };
    }

    private static double GetSeasonalFactorFromData(DateTime date, IEnumerable<SensorReading> historicalData)
    {
        try
        {
            // Filtrar dados válidos (energia > 0.08 kWh)
            var validData = historicalData.Where(r => r.Energy > 0.08).ToList();

            // Se não há dados válidos, usar fator sazonal padrão
            if (!validData.Any())
            {
                return GetDefaultSeasonalFactor(date);
            }

            // Analisar dados históricos para encontrar padrões sazonais reais
            var monthlyAverages = validData
                .GroupBy(r => r.Timestamp.Month)
                .Where(g => g.Any()) // Garantir que o grupo não está vazio
                .ToDictionary(g => g.Key, g => g.Average(r => r.Energy));

            // Se não há dados para o mês específico, usar fator padrão
            if (!monthlyAverages.ContainsKey(date.Month))
            {
                return GetDefaultSeasonalFactor(date);
            }

            // Calcular fator sazonal baseado nos dados reais
            var overallAverage = validData.Average(r => r.Energy);
            var monthAverage = monthlyAverages[date.Month];

            if (overallAverage > 0)
            {
                var seasonalFactor = monthAverage / overallAverage;

                // Limitar o fator sazonal a um range razoável (0.5 a 2.0)
                // para evitar distorções extremas
                return Math.Max(0.5, Math.Min(2.0, seasonalFactor));
            }

            return GetDefaultSeasonalFactor(date);
        }
        catch (Exception)
        {
            // Em caso de qualquer erro, retornar fator padrão
            return GetDefaultSeasonalFactor(date);
        }
    }

    private static double GetDefaultSeasonalFactor(DateTime date)
    {
        // Fator sazonal padrão baseado no mês
        return date.Month switch
        {
            12 or 1 or 2 => 1.15, // Verão - mais ar condicionado
            6 or 7 or 8 => 0.95,  // Inverno - menos ar condicionado
            _ => 1.0              // Outras estações
        };
    }

    // Métodos auxiliares para cálculos
    private static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var enumerable = values.ToList();
        if (!enumerable.Any()) return 0;

        var avg = enumerable.Average();
        var sumOfSquaresOfDifferences = enumerable.Select(val => (val - avg) * (val - avg)).Sum();
        return Math.Sqrt(sumOfSquaresOfDifferences / enumerable.Count);
    }

    private static int CalculateEfficiencyScore(IEnumerable<SensorReading> readings)
    {
        var readingsList = readings.ToList();
        if (!readingsList.Any()) return 0;

        // Score baseado em múltiplos fatores
        var totalReadings = readingsList.Count;
        var avgPower = readingsList.Average(r => r.Power);
        var powerStability = CalculatePowerStability(readingsList);
        var voltageStability = CalculateVoltageStability(readingsList);

        // Pontuação base (0-100)
        var baseScore = 100;

        // Bonificação por estabilidade de potência (0-20 pontos)
        var powerScore = (int)(powerStability * 20);

        // Bonificação por estabilidade de tensão (0-10 pontos)  
        var voltageScore = (int)(voltageStability * 10);

        // Bonificação por volume de dados (0-10 pontos)
        var dataScore = Math.Min(10, totalReadings / 100);

        return Math.Min(100, baseScore + powerScore + voltageScore + dataScore);
    }

    private static double CalculatePowerStability(IReadOnlyList<SensorReading> readings)
    {
        if (readings.Count < 2) return 0;

        var powerValues = readings.Select(r => r.Power).ToList();
        var avgPower = powerValues.Average();
        var stdDev = CalculateStandardDeviation(powerValues);

        // Coeficiente de variação inverso (mais estável = maior score)
        var coefficientOfVariation = avgPower > 0 ? stdDev / avgPower : 1;
        return Math.Max(0, 1 - Math.Min(1, coefficientOfVariation));
    }

    private static double CalculateVoltageStability(IReadOnlyList<SensorReading> readings)
    {
        if (readings.Count < 2) return 0;

        var voltageValues = readings.Select(r => r.Voltage).ToList();
        var avgVoltage = voltageValues.Average();

        // Penalizar desvios da tensão nominal (220V)
        var deviationFromNominal = Math.Abs(avgVoltage - 220) / 220;
        return Math.Max(0, 1 - deviationFromNominal);
    }

    private static double CalculateLinearTrend(IList<double> values)
    {
        if (values.Count < 2) return 0;

        var n = values.Count;
        var sumX = Enumerable.Range(0, n).Sum();
        var sumY = values.Sum();
        var sumXY = values.Select((y, i) => i * y).Sum();
        var sumXX = Enumerable.Range(0, n).Select(i => i * i).Sum();

        if (n * sumXX - sumX * sumX == 0) return 0;
        return (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
    }

    private static string GetEfficiencyRating(double avgPower)
    {
        return avgPower switch
        {
            < 50 => "Excelente",
            < 150 => "Bom",
            < 300 => "Regular",
            _ => "Precisa Melhorar"
        };
    }
}

// DTOs para as análises
public class EnergyDashboardDto
{
    public double TotalConsumption { get; set; }
    public double AverageDaily { get; set; }
    public decimal EstimatedMonthlyCost { get; set; }
    public int DevicesCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public string PeakHour { get; set; } = string.Empty;
    public int EfficiencyScore { get; set; }
    public double ComparedToLastMonth { get; set; }
    public List<HourlyConsumptionDto> HourlyConsumption { get; set; } = new();
    public List<DailyConsumptionDto> DailyConsumption { get; set; } = new();
}

public class EnergyPatternDto
{
    public string PatternName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double AverageConsumption { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string EfficiencyRating { get; set; } = string.Empty;
}

public class AnomalyDetectionDto
{
    public string DeviceName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double PowerValue { get; set; }
    public string ExpectedRange { get; set; } = string.Empty;
    public string AnomalyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class EnergyForecastDto
{
    public int ForecastDays { get; set; }
    public DateTime GeneratedAt { get; set; }
    public decimal Confidence { get; set; }
    public double TotalPredictedConsumption { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    public List<DailyForecastDto> DailyForecasts { get; set; } = new();
}

public class DailyForecastDto
{
    public DateTime Date { get; set; }
    public double PredictedConsumption { get; set; }
    public decimal EstimatedCost { get; set; }
    public string ConfidenceInterval { get; set; } = string.Empty;
}

public class DeviceRankingDto
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double TotalConsumption { get; set; }
    public double AveragePower { get; set; }
    public decimal EstimatedMonthlyCost { get; set; }
    public double OperatingHours { get; set; }
    public string EfficiencyRating { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; }
}

public class HourlyConsumptionDto
{
    public int Hour { get; set; }
    public double Consumption { get; set; }
}

public class DailyConsumptionDto
{
    public DateTime Date { get; set; }
    public double Consumption { get; set; }
}
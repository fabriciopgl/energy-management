using EnergyManagement.Application.Analytics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnergyManagement.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class AnalyticsController(IEnergyAnalysisApplicationService analysisService, ILogger<AnalyticsController> logger) : ControllerBase
{
    /// <summary>
    /// Obtém dados consolidados do dashboard energético baseados em dados reais
    /// </summary>
    /// <param name="startDate">Data inicial (opcional, padrão: 30 dias atrás)</param>
    /// <param name="endDate">Data final (opcional, padrão: hoje)</param>
    /// <returns>Dados analíticos do dashboard com métricas reais</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(EnergyDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            // Validar datas
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                return BadRequest("Data inicial não pode ser maior que data final");
            }

            // Limitar período máximo para performance
            if (startDate.HasValue && endDate.HasValue)
            {
                var daysDifference = (endDate.Value - startDate.Value).TotalDays;
                if (daysDifference > 365)
                {
                    return BadRequest("Período máximo permitido é de 365 dias");
                }
            }

            var result = await analysisService.GetDashboardAnalyticsAsync(userId.Value, startDate, endDate);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha ao obter dashboard analytics: {Message}", result.Message);
                return BadRequest(result.Message);
            }

            logger.LogInformation("Dashboard analytics obtido com sucesso para usuário {UserId}", userId.Value);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno ao obter dashboard analytics");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém padrões de consumo energético identificados por análise de dados reais
    /// </summary>
    /// <returns>Lista de padrões de consumo detectados nos dados históricos</returns>
    [HttpGet("patterns")]
    [ProducesResponseType(typeof(IReadOnlyList<EnergyPatternDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsumptionPatterns()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await analysisService.GetConsumptionPatternsAsync(userId.Value);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha ao obter padrões de consumo: {Message}", result.Message);
                return BadRequest(result.Message);
            }

            logger.LogInformation("Padrões de consumo obtidos: {Count} padrões identificados", result.Data?.Count ?? 0);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno ao obter padrões de consumo");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Detecta anomalias no consumo energético usando análise estatística dos dados reais
    /// </summary>
    /// <param name="days">Número de dias para análise (padrão: 30, máximo: 90)</param>
    /// <returns>Lista de anomalias detectadas nos dados</returns>
    [HttpGet("anomalies")]
    [ProducesResponseType(typeof(IReadOnlyList<AnomalyDetectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectAnomalies([FromQuery] int days = 30)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            if (days < 1 || days > 90)
                return BadRequest("O período deve estar entre 1 e 90 dias");

            var result = await analysisService.DetectAnomaliesAsync(userId.Value, days);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha ao detectar anomalias: {Message}", result.Message);
                return BadRequest(result.Message);
            }

            logger.LogInformation("Anomalias detectadas: {Count} anomalias encontradas em {Days} dias",
                result.Data?.Count ?? 0, days);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno ao detectar anomalias");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Gera previsão de consumo energético baseada em dados históricos reais
    /// </summary>
    /// <param name="days">Número de dias para previsão (padrão: 7, máximo: 30)</param>
    /// <returns>Previsão de consumo energético com base em tendências históricas</returns>
    [HttpGet("forecast")]
    [ProducesResponseType(typeof(EnergyForecastDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnergyForecast([FromQuery] int days = 7)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            if (days < 1 || days > 30)
                return BadRequest("O período de previsão deve estar entre 1 e 30 dias");

            var result = await analysisService.GetEnergyForecastAsync(userId.Value, days);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha ao gerar previsão: {Message}", result.Message);
                return BadRequest(result.Message);
            }

            logger.LogInformation("Previsão energética gerada: {Days} dias, confiança: {Confidence}%",
                days, result.Data?.Confidence * 100);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno ao gerar previsão");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém ranking de dispositivos por consumo energético baseado em dados reais
    /// </summary>
    /// <returns>Lista de dispositivos ordenada por consumo real dos últimos 30 dias</returns>
    [HttpGet("devices/ranking")]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceRankingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeviceRanking()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await analysisService.GetDeviceRankingAsync(userId.Value);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha ao obter ranking de dispositivos: {Message}", result.Message);
                return BadRequest(result.Message);
            }

            logger.LogInformation("Ranking de dispositivos obtido: {Count} dispositivos analisados",
                result.Data?.Count ?? 0);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno ao obter ranking de dispositivos");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém relatório completo de análise energética baseado em dados reais
    /// </summary>
    /// <param name="startDate">Data inicial do relatório</param>
    /// <param name="endDate">Data final do relatório</param>
    /// <returns>Relatório analítico completo com todas as métricas</returns>
    [HttpGet("report")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalyticsReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            // Validar datas
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                return BadRequest("Data inicial não pode ser maior que data final");
            }

            var reportStartTime = DateTime.UtcNow;

            // Obter todos os dados analíticos em paralelo para performance
            var dashboardTask = analysisService.GetDashboardAnalyticsAsync(userId.Value, startDate, endDate);
            var patternsTask = analysisService.GetConsumptionPatternsAsync(userId.Value);
            var anomaliesTask = analysisService.DetectAnomaliesAsync(userId.Value, 30);
            var forecastTask = analysisService.GetEnergyForecastAsync(userId.Value, 7);
            var rankingTask = analysisService.GetDeviceRankingAsync(userId.Value);

            await Task.WhenAll(dashboardTask, patternsTask, anomaliesTask, forecastTask, rankingTask);

            var reportGenerationTime = DateTime.UtcNow - reportStartTime;

            var report = new
            {
                GeneratedAt = DateTime.UtcNow,
                GenerationTimeMs = (int)reportGenerationTime.TotalMilliseconds,
                Period = new
                {
                    StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                    EndDate = endDate ?? DateTime.UtcNow
                },
                Dashboard = dashboardTask.Result.IsSuccess ? dashboardTask.Result.Data : null,
                Patterns = patternsTask.Result.IsSuccess ? patternsTask.Result.Data : null,
                Anomalies = anomaliesTask.Result.IsSuccess ? anomaliesTask.Result.Data : null,
                Forecast = forecastTask.Result.IsSuccess ? forecastTask.Result.Data : null,
                DeviceRanking = rankingTask.Result.IsSuccess ? rankingTask.Result.Data : null,
                Summary = new
                {
                    TotalAnomalies = anomaliesTask.Result.IsSuccess ? anomaliesTask.Result.Data?.Count ?? 0 : 0,
                    TotalPatterns = patternsTask.Result.IsSuccess ? patternsTask.Result.Data?.Count ?? 0 : 0,
                    TopConsumer = rankingTask.Result.IsSuccess ? rankingTask.Result.Data?.FirstOrDefault()?.DeviceName ?? "N/A" : "N/A",
                    DataQuality = new
                    {
                        DashboardSuccess = dashboardTask.Result.IsSuccess,
                        PatternsSuccess = patternsTask.Result.IsSuccess,
                        AnomaliesSuccess = anomaliesTask.Result.IsSuccess,
                        ForecastSuccess = forecastTask.Result.IsSuccess,
                        RankingSuccess = rankingTask.Result.IsSuccess
                    }
                }
            };

            logger.LogInformation("Relatório analítico completo gerado em {TimeMs}ms para usuário {UserId}",
                reportGenerationTime.TotalMilliseconds, userId.Value);

            return Ok(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno ao gerar relatório analítico");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim is not null && int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
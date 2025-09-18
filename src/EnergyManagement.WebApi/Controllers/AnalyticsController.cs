using EnergyManagement.Application.Analytics.Models.Dtos;
using EnergyManagement.Application.Analytics.Models.Requests;
using EnergyManagement.Application.Analytics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnergyManagement.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class AnalyticsController(IAnalyticsApplicationService analyticsService, ILogger<AnalyticsController> logger) : ControllerBase
{

    /// <summary>
    /// Analisa o consumo energético do usuário com algoritmos de ML
    /// </summary>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(EnergyAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeConsumption([FromBody] AnalyzeConsumptionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await analyticsService.AnalyzeConsumptionAsync(userId.Value, request);

            if (result.IsFailure)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao analisar consumo para usuário {UserId}", GetCurrentUserId());
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém padrões de consumo identificados pelo algoritmo de clustering
    /// </summary>
    [HttpGet("patterns")]
    [ProducesResponseType(typeof(List<ConsumptionPatternDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsumptionPatterns()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await analyticsService.GetConsumptionPatternsAsync(userId.Value);

            if (result.IsFailure)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter padrões de consumo para usuário {UserId}", GetCurrentUserId());
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém anomalias detectadas pelo algoritmo de detecção de anomalias
    /// </summary>
    [HttpGet("anomalies")]
    [ProducesResponseType(typeof(List<AnomalyDetectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnomalies([FromQuery] bool includeResolved = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await analyticsService.GetAnomaliesAsync(userId.Value, includeResolved);

            if (result.IsFailure)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter anomalias para usuário {UserId}", GetCurrentUserId());
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém recomendações de economia energética geradas por ML
    /// </summary>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(List<EnergyRecommendationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendations([FromQuery] bool includeApplied = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await analyticsService.GetRecommendationsAsync(userId.Value, includeApplied);

            if (result.IsFailure)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter recomendações para usuário {UserId}", GetCurrentUserId());
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Marca uma anomalia como resolvida
    /// </summary>
    [HttpPost("anomalies/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveAnomaly([FromBody] ResolveAnomalyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await analyticsService.ResolveAnomalyAsync(userId.Value, request);

            if (result.IsFailure)
                return BadRequest(result.Message);

            return Ok(new { message = "Anomalia marcada como resolvida" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao resolver anomalia {AnomalyId} para usuário {UserId}",
                request.AnomalyId, GetCurrentUserId());
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Marca uma recomendação como aplicada
    /// </summary>
    [HttpPost("recommendations/{id:int}/apply")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyRecommendation(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await analyticsService.ApplyRecommendationAsync(userId.Value, id);

            if (result.IsFailure)
                return BadRequest(result.Message);

            return Ok(new { message = "Recomendação marcada como aplicada" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao aplicar recomendação {RecommendationId} para usuário {UserId}",
                id, GetCurrentUserId());
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Executa análise completa de ML (clustering, anomalias, recomendações)
    /// </summary>
    [HttpPost("run-analysis")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RunAnalysis()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await analyticsService.RunAnalysisAsync(userId.Value);

            if (result.IsFailure)
                return BadRequest(result.Message);

            return Ok(new { message = "Análise de ML executada com sucesso" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao executar análise para usuário {UserId}", GetCurrentUserId());
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém dashboard com métricas gerais e insights de ML
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            // Análise básica dos últimos 30 dias
            var analysisRequest = new AnalyzeConsumptionRequest
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                IncludeAnomalies = true,
                IncludeRecommendations = true,
                IncludePredictions = true
            };

            var analysisResult = await analyticsService.AnalyzeConsumptionAsync(userId.Value, analysisRequest);

            if (analysisResult.IsFailure)
                return BadRequest(analysisResult.Message);

            var analysis = analysisResult.Data!;

            var dashboard = new
            {
                Summary = new
                {
                    TotalConsumption = analysis.TotalConsumption,
                    AverageDaily = analysis.AverageDaily,
                    AverageHourly = analysis.AverageHourly,
                    AnalysisDate = analysis.AnalysisDate
                },
                Patterns = analysis.Patterns.Take(4),
                RecentAnomalies = analysis.Anomalies.Where(a => !a.IsResolved).Take(5),
                TopRecommendations = analysis.Recommendations.Where(r => !r.IsApplied).Take(3),
                Trend = analysis.Trend,
                Insights = GenerateInsights(analysis)
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter dashboard para usuário {UserId}", GetCurrentUserId());
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private object GenerateInsights(EnergyAnalysisDto analysis)
    {
        var insights = new List<string>();

        // Insight sobre padrões
        if (analysis.Patterns.Any())
        {
            var highestPattern = analysis.Patterns.OrderByDescending(p => p.AverageConsumption).First();
            insights.Add($"Seu maior consumo ocorre no período: {highestPattern.PatternType}");
        }

        // Insight sobre economia potencial
        if (analysis.Recommendations.Any())
        {
            var totalSavings = analysis.Recommendations.Sum(r => r.PotentialSavingsPercent);
            insights.Add($"Você pode economizar até {totalSavings:F1}% aplicando nossas recomendações");
        }

        // Insight sobre anomalias
        var unresolvedAnomalies = analysis.Anomalies.Count(a => !a.IsResolved);
        if (unresolvedAnomalies > 0)
        {
            insights.Add($"Detectamos {unresolvedAnomalies} anomalia(s) que merecem atenção");
        }

        return new
        {
            MainInsights = insights.Take(3),
            Score = CalculateEfficiencyScore(analysis),
            LastAnalysis = analysis.AnalysisDate
        };
    }

    private int CalculateEfficiencyScore(EnergyAnalysisDto analysis)
    {
        var score = 100;

        // Penalizar por anomalias não resolvidas
        score -= analysis.Anomalies.Count(a => !a.IsResolved) * 5;

        // Bonificar por recomendações aplicadas
        score += analysis.Recommendations.Count(r => r.IsApplied) * 2;

        // Garantir que o score fique entre 0 e 100
        return Math.Max(0, Math.Min(100, score));
    }
}

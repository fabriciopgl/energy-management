using EnergyManagement.Application.Analytics.Domain;
using EnergyManagement.Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagement.Infraestructure.Repositories;

public class AnalyticsRepository(AppDbContext context) : IAnalyticsRepository
{
    public async Task<IReadOnlyList<ConsumptionPattern>> GetUserPatternsAsync(int userId)
    {
        return await context.Set<ConsumptionPattern>()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.AnalyzedAt)
            .Take(10) // Últimos 10 padrões
            .ToListAsync();
    }

    public async Task SavePatternAsync(ConsumptionPattern pattern)
    {
        // Verificar se já existe um padrão similar recente para evitar duplicatas
        var existingPattern = await context.Set<ConsumptionPattern>()
            .FirstOrDefaultAsync(p => p.UserId == pattern.UserId &&
                                     p.PatternType == pattern.PatternType &&
                                     p.AnalyzedAt.Date == pattern.AnalyzedAt.Date);

        if (existingPattern != null)
        {
            // Atualizar padrão existente
            existingPattern.AverageConsumption = pattern.AverageConsumption;
            existingPattern.PeakConsumption = pattern.PeakConsumption;
            existingPattern.AnalyzedAt = pattern.AnalyzedAt;
            existingPattern.DeviceNames = pattern.DeviceNames;
            context.Set<ConsumptionPattern>().Update(existingPattern);
        }
        else
        {
            await context.Set<ConsumptionPattern>().AddAsync(pattern);
        }

        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AnomalyDetection>> GetUserAnomaliesAsync(int userId, bool includeResolved = false)
    {
        var query = context.Set<AnomalyDetection>()
            .Where(a => a.UserId == userId);

        if (!includeResolved)
        {
            query = query.Where(a => !a.IsResolved);
        }

        return await query
            .OrderByDescending(a => a.DetectedAt)
            .Take(50) // Últimas 50 anomalias
            .ToListAsync();
    }

    public async Task SaveAnomalyAsync(AnomalyDetection anomaly)
    {
        // Verificar se já existe uma anomalia similar recente para evitar duplicatas
        var existingAnomaly = await context.Set<AnomalyDetection>()
            .FirstOrDefaultAsync(a => a.UserId == anomaly.UserId &&
                                     a.AnomalyType == anomaly.AnomalyType &&
                                     a.DeviceId == anomaly.DeviceId &&
                                     a.DetectedAt.Date == anomaly.DetectedAt.Date);

        if (existingAnomaly == null)
        {
            await context.Set<AnomalyDetection>().AddAsync(anomaly);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<EnergyRecommendation>> GetUserRecommendationsAsync(int userId, bool includeApplied = false)
    {
        var query = context.Set<EnergyRecommendation>()
            .Where(r => r.UserId == userId);

        if (!includeApplied)
        {
            query = query.Where(r => !r.IsApplied);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(20) // Últimas 20 recomendações
            .ToListAsync();
    }

    public async Task SaveRecommendationAsync(EnergyRecommendation recommendation)
    {
        // Verificar se já existe uma recomendação similar recente
        var existingRecommendation = await context.Set<EnergyRecommendation>()
            .FirstOrDefaultAsync(r => r.UserId == recommendation.UserId &&
                                     r.RecommendationType == recommendation.RecommendationType &&
                                     r.CreatedAt.Date == recommendation.CreatedAt.Date);

        if (existingRecommendation == null)
        {
            await context.Set<EnergyRecommendation>().AddAsync(recommendation);
            await context.SaveChangesAsync();
        }
    }

    public async Task MarkAnomalyAsResolvedAsync(int anomalyId)
    {
        var anomaly = await context.Set<AnomalyDetection>()
            .FirstOrDefaultAsync(a => a.Id == anomalyId);

        if (anomaly != null)
        {
            anomaly.IsResolved = true;
            anomaly.ResolvedAt = DateTime.UtcNow;
            context.Set<AnomalyDetection>().Update(anomaly);
            await context.SaveChangesAsync();
        }
    }

    public async Task MarkRecommendationAsAppliedAsync(int recommendationId)
    {
        var recommendation = await context.Set<EnergyRecommendation>()
            .FirstOrDefaultAsync(r => r.Id == recommendationId);

        if (recommendation != null)
        {
            recommendation.IsApplied = true;
            recommendation.AppliedAt = DateTime.UtcNow;
            context.Set<EnergyRecommendation>().Update(recommendation);
            await context.SaveChangesAsync();
        }
    }
}

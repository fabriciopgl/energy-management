namespace EnergyManagement.Application.Analytics.Models.Dtos;

public class EnergyAnalysisDto
{
    public DateTime AnalysisDate { get; set; }
    public double TotalConsumption { get; set; }
    public double AverageDaily { get; set; }
    public double AverageHourly { get; set; }
    public List<ConsumptionPatternDto> Patterns { get; set; } = new();
    public List<AnomalyDetectionDto> Anomalies { get; set; } = new();
    public List<EnergyRecommendationDto> Recommendations { get; set; } = new();
    public ConsumptionTrendDto Trend { get; set; } = new();
}

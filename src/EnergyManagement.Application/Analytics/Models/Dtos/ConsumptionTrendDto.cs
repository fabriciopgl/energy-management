namespace EnergyManagement.Application.Analytics.Models.Dtos;

public class ConsumptionTrendDto
{
    public string TrendDirection { get; set; } = string.Empty; // "Increasing", "Decreasing", "Stable"
    public double TrendPercentage { get; set; }
    public string Period { get; set; } = string.Empty; // "Last 7 days", "Last 30 days"
    public List<HourlyConsumptionDto> HourlyData { get; set; } = new();
    public List<DailyConsumptionDto> DailyData { get; set; } = new();
}
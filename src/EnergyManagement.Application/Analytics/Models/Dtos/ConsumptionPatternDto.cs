namespace EnergyManagement.Application.Analytics.Models.Dtos;

public class ConsumptionPatternDto
{
    public int Id { get; set; }
    public string PatternType { get; set; } = string.Empty;
    public double AverageConsumption { get; set; }
    public double PeakConsumption { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int ClusterId { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public List<string> DeviceNames { get; set; } = new();
}
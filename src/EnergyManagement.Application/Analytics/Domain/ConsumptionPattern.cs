using EnergyManagement.Application.Users.Domain;

namespace EnergyManagement.Application.Analytics.Domain;

public class ConsumptionPattern
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PatternType { get; set; } = string.Empty; // "Morning", "Afternoon", "Evening", "Night"
    public double AverageConsumption { get; set; }
    public double PeakConsumption { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int ClusterId { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public string DeviceNames { get; set; } = string.Empty; // JSON array of device names

    // Navegação
    public User User { get; set; } = null!;
}
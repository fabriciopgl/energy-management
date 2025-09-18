namespace EnergyManagement.Application.Analytics.Models.Dtos;

public class AnomalyDetectionDto
{
    public int Id { get; set; }
    public int? DeviceId { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public double Value { get; set; }
    public double ExpectedValue { get; set; }
    public double AnomalyScore { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string Severity { get; set; } = string.Empty; // "Low", "Medium", "High"
}
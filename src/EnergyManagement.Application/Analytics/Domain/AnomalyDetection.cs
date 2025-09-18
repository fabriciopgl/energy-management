using EnergyManagement.Application.Users.Domain;

namespace EnergyManagement.Application.Analytics.Domain;

public class AnomalyDetection
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? DeviceId { get; set; }
    public string AnomalyType { get; set; } = string.Empty; // "High_Consumption", "Unusual_Pattern", "Device_Malfunction"
    public double Value { get; set; }
    public double ExpectedValue { get; set; }
    public double AnomalyScore { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Navegação
    public User User { get; set; } = null!;
}

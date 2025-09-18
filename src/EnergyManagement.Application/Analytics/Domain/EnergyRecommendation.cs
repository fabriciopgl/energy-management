using EnergyManagement.Application.Users.Domain;

namespace EnergyManagement.Application.Analytics.Domain;

public class EnergyRecommendation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RecommendationType { get; set; } = string.Empty; // "Time_Shift", "Usage_Reduction", "Device_Optimization"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double PotentialSavings { get; set; } // Economia estimada em kWh
    public double PotentialSavingsPercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsApplied { get; set; }
    public DateTime? AppliedAt { get; set; }

    // Navegação
    public User User { get; set; } = null!;
}
namespace EnergyManagement.Application.Analytics.Models.Dtos;

public class EnergyRecommendationDto
{
    public int Id { get; set; }
    public string RecommendationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double PotentialSavings { get; set; }
    public double PotentialSavingsPercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsApplied { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string Priority { get; set; } = string.Empty; // "Low", "Medium", "High"
}

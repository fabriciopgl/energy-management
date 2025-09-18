namespace EnergyManagement.Application.Analytics.Models.Requests;

public class AnalyzeConsumptionRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<int> DeviceIds { get; set; } = new();
    public bool IncludeAnomalies { get; set; } = true;
    public bool IncludeRecommendations { get; set; } = true;
    public bool IncludePredictions { get; set; } = true;
}

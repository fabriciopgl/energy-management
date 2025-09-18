namespace EnergyManagement.Application.Analytics.Models.Dtos;

public class HourlyConsumptionDto
{
    public DateTime Hour { get; set; }
    public double Consumption { get; set; }
    public double PredictedConsumption { get; set; }
}

namespace EnergyManagement.Application.Analytics.Models.Dtos;

public class DailyConsumptionDto
{
    public DateTime Date { get; set; }
    public double Consumption { get; set; }
    public double PredictedConsumption { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
}

namespace EnergyManagement.Application.Sensors.Models.Dtos;

public class DailyAggregation
{
    public DateTime Date { get; set; }
    public double TotalPower { get; set; }
    public double TotalEnergy { get; set; }
    public double AverageCurrent { get; set; }
    public double AverageVoltage { get; set; }
    public int ReadingsCount { get; set; }
    public double MaxPower { get; set; }
    public double MinPower { get; set; }
}

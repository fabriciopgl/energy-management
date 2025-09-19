namespace EnergyManagement.Application.Sensors.Models.Dtos;

public class HourlyAggregation
{
    public int Hour { get; set; }
    public double TotalPower { get; set; }
    public double TotalEnergy { get; set; }
    public double AverageCurrent { get; set; }
    public double AverageVoltage { get; set; }
    public int ReadingsCount { get; set; }
}

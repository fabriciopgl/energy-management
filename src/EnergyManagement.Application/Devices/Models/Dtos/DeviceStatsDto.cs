namespace EnergyManagement.Application.Devices.Models.Dtos;

public class DeviceStatsDto
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public int TotalReadings { get; set; }
    public DateTime? LastReading { get; set; }
    public double AveragePower { get; set; }
    public double TotalEnergy { get; set; }
    public string Status { get; set; } = string.Empty;
}
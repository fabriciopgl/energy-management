using System.ComponentModel.DataAnnotations;

namespace EnergyManagement.Application.Sensors.Models.Dtos;

public class DeviceHeartbeatDto
{
    [Required]
    public string MacAddress { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int? SignalStrength { get; set; }
    public int? BatteryLevel { get; set; }
    public double? Temperature { get; set; }
    public string? Status { get; set; } = "online";
    public string? Version { get; set; }
}

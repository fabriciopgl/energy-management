using System.ComponentModel.DataAnnotations;

namespace EnergyManagement.Application.Sensors.Models.Dtos;

public class SensorDataDto
{
    [Required]
    public string MacAddress { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue)]
    public double Current { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public double Voltage { get; set; }

    public double Power => Current * Voltage;

    [Required]
    public DateTime Timestamp { get; set; }

    // Dados adicionais do ESP32
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public int? SignalStrength { get; set; } // RSSI em dBm
    public int? BatteryLevel { get; set; } // Percentual de bateria
    public string? SensorVersion { get; set; }
}

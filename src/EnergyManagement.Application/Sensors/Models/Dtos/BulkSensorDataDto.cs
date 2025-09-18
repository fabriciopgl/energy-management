using System.ComponentModel.DataAnnotations;

namespace EnergyManagement.Application.Sensors.Models.Dtos;

public class BulkSensorDataDto
{
    [Required]
    public string MacAddress { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<SensorReadingDto> Readings { get; set; } = new();
}

using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Application.Users.Domain;

namespace EnergyManagement.Application.Devices.Domain;

public class Device
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }

    // Foreign Key
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Navigation Properties
    public ICollection<SensorReading> SensorReadings { get; set; } = [];
}

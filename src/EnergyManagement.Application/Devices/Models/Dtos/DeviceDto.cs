namespace EnergyManagement.Application.Devices.Models.Dtos;

public class DeviceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public int UserId { get; set; }
    public string Status => IsActive ? (LastSeenAt.HasValue && LastSeenAt > DateTime.UtcNow.AddMinutes(-5) ? "Online" : "Offline") : "Inactive";
}

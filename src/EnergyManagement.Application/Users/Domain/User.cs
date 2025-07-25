using EnergyManagement.Application.Devices.Domain;
using Microsoft.AspNetCore.Identity;

namespace EnergyManagement.Application.Users.Domain;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<Device> Devices { get; set; } = [];
    public ICollection<UserPreference> Preferences { get; set; } = [];

    // Computed Properties
    public string FullName => $"{FirstName} {LastName}".Trim();
}
namespace EnergyManagement.Application.Users.Models.Dtos;

public class UserStatsDto
{
    public int TotalDevices { get; set; }
    public int ActiveDevices { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime MemberSince { get; set; }
    public int TotalPreferences { get; set; }
}
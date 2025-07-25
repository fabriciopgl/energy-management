namespace EnergyManagement.Application.Users.Domain;

public class UserPreference
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign Key
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

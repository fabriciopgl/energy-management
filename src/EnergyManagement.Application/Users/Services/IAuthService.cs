using EnergyManagement.Application.Users.Domain;

namespace EnergyManagement.Application.Users.Services;

public interface IAuthService
{
    Task<string> GenerateJwtTokenAsync(User user);
    Task<bool> ValidateTokenAsync(string token);
    Task<int?> GetUserIdFromTokenAsync(string token);
}

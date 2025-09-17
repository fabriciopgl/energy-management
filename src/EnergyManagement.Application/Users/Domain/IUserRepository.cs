using EnergyManagement.Core.Common;

namespace EnergyManagement.Application.Users.Domain;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> GetAllActiveAsync();
    Task UpdateLastLoginAsync(int userId);
    Task<bool> EmailExistsAsync(string email);
}

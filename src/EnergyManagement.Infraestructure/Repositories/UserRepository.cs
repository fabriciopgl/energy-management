using EnergyManagement.Application.Users.Domain;
using EnergyManagement.Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagement.Infraestructure.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(int id)
    {
        return await context.Users
            .Include(u => u.Devices)
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await context.Users
            .Include(u => u.Devices)
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await context.Users
            .Include(u => u.Devices)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<User>> GetAllActiveAsync()
    {
        return await context.Users
            .Where(u => u.IsActive)
            .Include(u => u.Devices)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<User> AddAsync(User entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        context.Users.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(User entity)
    {
        context.Users.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User entity)
    {
        context.Users.Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return;

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }
}

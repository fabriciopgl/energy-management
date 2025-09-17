using EnergyManagement.Application.Devices.Domain;
using EnergyManagement.Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagement.Infraestructure.Repositories;

public class DeviceRepository(AppDbContext context) : IDeviceRepository
{
    public async Task<Device?> GetByIdAsync(int id)
    {
        return await context.Devices
            .Include(d => d.User)
            .Include(d => d.SensorReadings)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Device?> GetByMacAddressAsync(string macAddress)
    {
        return await context.Devices
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.MacAddress == macAddress.ToUpperInvariant());
    }

    public async Task<IReadOnlyList<Device>> GetAllAsync()
    {
        return await context.Devices
            .Include(d => d.User)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Device>> GetByUserIdAsync(int userId)
    {
        return await context.Devices
            .Where(d => d.UserId == userId)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Device>> GetActiveByUserIdAsync(int userId)
    {
        return await context.Devices
            .Where(d => d.UserId == userId && d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<Device> AddAsync(Device entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        context.Devices.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Device entity)
    {
        context.Devices.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Device entity)
    {
        context.Devices.Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await context.Devices.AnyAsync(d => d.Id == id);
    }

    public async Task<bool> MacAddressExistsAsync(string macAddress)
    {
        return await context.Devices.AnyAsync(d => d.MacAddress == macAddress.ToUpperInvariant());
    }

    public async Task<bool> UserOwnsDeviceAsync(int userId, int deviceId)
    {
        return await context.Devices.AnyAsync(d => d.Id == deviceId && d.UserId == userId);
    }

    public async Task UpdateLastSeenAsync(int deviceId)
    {
        var device = await context.Devices.FindAsync(deviceId);
        if (device is null) return;

        device.LastSeenAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }
}

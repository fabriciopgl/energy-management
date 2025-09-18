using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagement.Infraestructure.Repositories;

public class SensorReadingRepository(AppDbContext db) : ISensorReadingRepository
{
    public async Task AddAsync(SensorReading reading)
    {
        db.SensorReadings.Add(reading);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<SensorReading>> ListAsync(int limit = 100)
    {
        return await db.SensorReadings
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SensorReading>> GetReadingsByUserAndPeriodAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await db.SensorReadings
            .Include(r => r.Device)
            .Where(r => r.Device != null && r.Device.UserId == userId &&
                       r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SensorReading>> GetReadingsByDeviceAndPeriodAsync(int deviceId, DateTime startDate, DateTime endDate)
    {
        return await db.SensorReadings
            .Where(r => r.DeviceId == deviceId &&
                       r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();
    }

    public async Task<double> GetAverageConsumptionByUserAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var readings = await GetReadingsByUserAndPeriodAsync(userId, startDate, endDate);

        if (!readings.Any())
            return 0;

        return readings.Average(r => r.Current * r.Voltage);
    }

    public async Task<IReadOnlyList<SensorReading>> GetRecentReadingsByUserAsync(int userId, int hours = 24)
    {
        var startDate = DateTime.UtcNow.AddHours(-hours);
        return await GetReadingsByUserAndPeriodAsync(userId, startDate, DateTime.UtcNow);
    }

    public async Task AddRangeAsync(IEnumerable<SensorReading> readings)
    {
        await db.SensorReadings.AddRangeAsync(readings);
        await db.SaveChangesAsync();
    }
}

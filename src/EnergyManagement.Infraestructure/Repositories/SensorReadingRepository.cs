using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Application.Sensors.Models.Dtos;
using EnergyManagement.Infraestructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SensorReading>> GetByDeviceIdAsync(int deviceId, int limit = 1000)
    {
        return await db.SensorReadings
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SensorReading>> GetByDeviceIdsAsync(IEnumerable<int> deviceIds, int limit = 1000)
    {
        return await db.SensorReadings
            .Where(r => deviceIds.Any(a => r.DeviceId == a))
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SensorReading>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 10000)
    {
        return await db.SensorReadings
            .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SensorReading>> GetByDeviceAndDateRangeAsync(int deviceId, DateTime startDate, DateTime endDate)
    {
        return await db.SensorReadings
            .Where(r => r.DeviceId == deviceId && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SensorReading>> GetByDevicesAndDateRangeAsync(IEnumerable<int> deviceIds, DateTime startDate, DateTime endDate)
    {
        return await db.SensorReadings
            .Where(r => deviceIds.Any(a => r.DeviceId == a) && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<SensorReading?> GetLatestByDeviceIdAsync(int deviceId)
    {
        return await db.SensorReadings
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<DateTime?> GetFirstReadingDateAsync()
    {
        return await db.SensorReadings
            .OrderBy(r => r.Timestamp)
            .Select(r => r.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetTotalReadingsCountAsync()
    {
        return await db.SensorReadings.CountAsync();
    }

    public async Task<int> GetReadingsCountByDeviceAsync(int deviceId)
    {
        return await db.SensorReadings
            .Where(r => r.DeviceId == deviceId)
            .CountAsync();
    }

    public async Task<IReadOnlyList<HourlyAggregation>> GetHourlyAggregationsAsync(IEnumerable<int> deviceIds, DateTime startDate, DateTime endDate)
    {
        return await db.SensorReadings
            .Where(r => deviceIds.Any(a => r.DeviceId == a) && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .GroupBy(r => r.Timestamp.Hour)
            .Select(g => new HourlyAggregation
            {
                Hour = g.Key,
                TotalPower = g.Sum(r => r.Power),
                TotalEnergy = g.Sum(r => r.Energy),
                AverageCurrent = g.Average(r => r.Current),
                AverageVoltage = g.Average(r => r.Voltage),
                ReadingsCount = g.Count()
            })
            .OrderBy(h => h.Hour)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<DailyAggregation>> GetDailyAggregationsAsync(IEnumerable<int> deviceIds, DateTime startDate, DateTime endDate)
    {
        return await db.SensorReadings
            .Where(r => deviceIds.Any(a => r.DeviceId == a) && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .GroupBy(r => r.Timestamp.Date)
            .Select(g => new DailyAggregation
            {
                Date = g.Key,
                TotalPower = g.Sum(r => r.Power),
                TotalEnergy = g.Sum(r => r.Energy),
                AverageCurrent = g.Average(r => r.Current),
                AverageVoltage = g.Average(r => r.Voltage),
                MaxPower = g.Max(r => r.Power),
                MinPower = g.Min(r => r.Power),
                ReadingsCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .AsNoTracking()
            .ToListAsync();
    }
}

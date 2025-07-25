using EnergyManagementApi.Data;
using EnergyManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagementApi.Repositories;

public class ReadingRepository(AppDbContext db) : IReadingRepository
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
}

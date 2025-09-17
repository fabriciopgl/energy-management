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
            .AsNoTracking()
            .ToListAsync();
    }
}

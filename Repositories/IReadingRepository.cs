using EnergyManagementApi.Models;

namespace EnergyManagementApi.Repositories;

public interface IReadingRepository
{
    Task AddAsync(SensorReading reading);
    Task<IReadOnlyList<SensorReading>> ListAsync(int limit = 100);
}

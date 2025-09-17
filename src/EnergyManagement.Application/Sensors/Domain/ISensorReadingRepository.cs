namespace EnergyManagement.Application.Sensors.Domain;

public interface ISensorReadingRepository
{
    Task AddAsync(SensorReading reading);
    Task<IReadOnlyList<SensorReading>> ListAsync(int limit = 100);
}

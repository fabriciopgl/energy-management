namespace EnergyManagement.Application.Sensors.Domain;

public interface ISensorReadingRepository
{
    Task AddAsync(SensorReading reading);
    Task AddRangeAsync(IEnumerable<SensorReading> readings);
    Task<IReadOnlyList<SensorReading>> ListAsync(int limit = 100);
    Task<IReadOnlyList<SensorReading>> GetReadingsByUserAndPeriodAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<SensorReading>> GetReadingsByDeviceAndPeriodAsync(int deviceId, DateTime startDate, DateTime endDate);
    Task<double> GetAverageConsumptionByUserAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<SensorReading>> GetRecentReadingsByUserAsync(int userId, int hours = 24);
}

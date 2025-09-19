using EnergyManagement.Application.Sensors.Models.Dtos;

namespace EnergyManagement.Application.Sensors.Domain;

public interface ISensorReadingRepository
{
    Task AddAsync(SensorReading reading);
    Task<IReadOnlyList<SensorReading>> ListAsync(int limit = 100);

    // Novos métodos para análises específicas
    Task<IReadOnlyList<SensorReading>> GetByDeviceIdAsync(int deviceId, int limit = 1000);
    Task<IReadOnlyList<SensorReading>> GetByDeviceIdsAsync(IEnumerable<int> deviceIds, int limit = 1000);
    Task<IReadOnlyList<SensorReading>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 10000);
    Task<IReadOnlyList<SensorReading>> GetByDeviceAndDateRangeAsync(int deviceId, DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<SensorReading>> GetByDevicesAndDateRangeAsync(IEnumerable<int> deviceIds, DateTime startDate, DateTime endDate);

    // Métodos para agregações
    Task<SensorReading?> GetLatestByDeviceIdAsync(int deviceId);
    Task<DateTime?> GetFirstReadingDateAsync();
    Task<int> GetTotalReadingsCountAsync();
    Task<int> GetReadingsCountByDeviceAsync(int deviceId);

    // Métodos para análises temporais
    Task<IReadOnlyList<HourlyAggregation>> GetHourlyAggregationsAsync(IEnumerable<int> deviceIds, DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<DailyAggregation>> GetDailyAggregationsAsync(IEnumerable<int> deviceIds, DateTime startDate, DateTime endDate);
}

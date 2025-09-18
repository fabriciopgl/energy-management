using EnergyManagement.Application.Sensors.Models.Dtos;
using EnergyManagement.Core.Common;

namespace EnergyManagement.Application.Sensors.Services;

public interface ISensorDataService
{
    Task<Result> ProcessSensorDataAsync(SensorDataDto sensorData);
    Task<Result> ProcessBulkSensorDataAsync(BulkSensorDataDto bulkData);
    Task<Result> ValidateDeviceAsync(string macAddress);
    Task<Result> UpdateDeviceStatusAsync(string macAddress, bool isOnline);
}

using EnergyManagement.Application.Devices.Models.Dtos;
using EnergyManagement.Core.Common;

namespace EnergyManagement.Application.Devices.Services;

public interface IDeviceApplicationService
{
    Task<Result<DeviceDto>> CreateAsync(int userId, CreateDeviceRequestDto request);
    Task<Result<DeviceDto>> UpdateAsync(int userId, int deviceId, UpdateDeviceRequestDto request);
    Task<Result<bool>> DeleteAsync(int userId, int deviceId);
    Task<Result<DeviceDto>> GetByIdAsync(int userId, int deviceId);
    Task<Result<IReadOnlyList<DeviceDto>>> GetAllByUserAsync(int userId);
    Task<Result<IReadOnlyList<DeviceStatsDto>>> GetStatsAsync(int userId);
}
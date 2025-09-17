using EnergyManagement.Core.Common;

namespace EnergyManagement.Application.Devices.Domain;

public interface IDeviceRepository : IRepository<Device>
{
    Task<Device?> GetByMacAddressAsync(string macAddress);
    Task<IReadOnlyList<Device>> GetByUserIdAsync(int userId);
    Task<IReadOnlyList<Device>> GetActiveByUserIdAsync(int userId);
    Task<bool> MacAddressExistsAsync(string macAddress);
    Task<bool> UserOwnsDeviceAsync(int userId, int deviceId);
    Task UpdateLastSeenAsync(int deviceId);
}

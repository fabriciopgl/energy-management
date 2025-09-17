using EnergyManagement.Application.Devices.Domain;
using EnergyManagement.Application.Devices.Models.Dtos;
using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Core.Common;

namespace EnergyManagement.Application.Devices.Services;

public class DeviceApplicationService : IDeviceApplicationService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ISensorReadingRepository _sensorRepository;

    public DeviceApplicationService(IDeviceRepository deviceRepository, ISensorReadingRepository sensorRepository)
    {
        _deviceRepository = deviceRepository;
        _sensorRepository = sensorRepository;
    }

    public async Task<Result<DeviceDto>> CreateAsync(int userId, CreateDeviceRequestDto request)
    {
        var macExists = await _deviceRepository.MacAddressExistsAsync(request.MacAddress);
        if (macExists)
            return Result<DeviceDto>.Failure(
                "MAC Address já está em uso",
                ["Este dispositivo já foi cadastrado no sistema"]);

        var device = new Device
        {
            Name = request.Name,
            MacAddress = request.MacAddress.ToUpperInvariant(),
            Location = request.Location,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var createdDevice = await _deviceRepository.AddAsync(device);
        var deviceDto = MapToDeviceDto(createdDevice);

        return Result<DeviceDto>.Success(deviceDto, "Dispositivo criado com sucesso");
    }

    public async Task<Result<DeviceDto>> UpdateAsync(int userId, int deviceId, UpdateDeviceRequestDto request)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId);
        if (device is null)
            return Result<DeviceDto>.Failure("Dispositivo não encontrado");

        var userOwns = await _deviceRepository.UserOwnsDeviceAsync(userId, deviceId);
        if (!userOwns)
            return Result<DeviceDto>.Failure("Acesso negado", ["Você não tem permissão para este dispositivo"]);

        device.Name = request.Name;
        device.Location = request.Location;
        device.IsActive = request.IsActive;

        await _deviceRepository.UpdateAsync(device);
        var deviceDto = MapToDeviceDto(device);

        return Result<DeviceDto>.Success(deviceDto, "Dispositivo atualizado com sucesso");
    }

    public async Task<Result<bool>> DeleteAsync(int userId, int deviceId)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId);
        if (device is null)
            return Result<bool>.Failure("Dispositivo não encontrado");

        var userOwns = await _deviceRepository.UserOwnsDeviceAsync(userId, deviceId);
        if (!userOwns)
            return Result<bool>.Failure("Acesso negado", ["Você não tem permissão para este dispositivo"]);

        await _deviceRepository.DeleteAsync(device);
        return Result<bool>.Success(true, "Dispositivo removido com sucesso");
    }

    public async Task<Result<DeviceDto>> GetByIdAsync(int userId, int deviceId)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId);
        if (device is null)
            return Result<DeviceDto>.Failure("Dispositivo não encontrado");

        var userOwns = await _deviceRepository.UserOwnsDeviceAsync(userId, deviceId);
        if (!userOwns)
            return Result<DeviceDto>.Failure("Acesso negado", ["Você não tem permissão para este dispositivo"]);

        var deviceDto = MapToDeviceDto(device);
        return Result<DeviceDto>.Success(deviceDto);
    }

    public async Task<Result<IReadOnlyList<DeviceDto>>> GetAllByUserAsync(int userId)
    {
        var devices = await _deviceRepository.GetByUserIdAsync(userId);
        var deviceDtos = devices.Select(MapToDeviceDto).ToList();

        return Result<IReadOnlyList<DeviceDto>>.Success(deviceDtos);
    }

    public async Task<Result<IReadOnlyList<DeviceStatsDto>>> GetStatsAsync(int userId)
    {
        var devices = await _deviceRepository.GetByUserIdAsync(userId);
        var stats = new List<DeviceStatsDto>();

        foreach (var device in devices)
        {
            var readings = await _sensorRepository.ListAsync(100); // Implementar filtro por dispositivo depois
            var deviceReadings = readings.Where(r => r.DeviceId == device.Id).ToList();

            var stat = new DeviceStatsDto
            {
                DeviceId = device.Id,
                DeviceName = device.Name,
                TotalReadings = deviceReadings.Count,
                LastReading = deviceReadings.Max(r => r.Timestamp),
                AveragePower = deviceReadings.Any() ? deviceReadings.Average(r => r.Power) : 0,
                TotalEnergy = deviceReadings.Sum(r => r.Energy),
                Status = device.IsActive ? (device.LastSeenAt.HasValue && device.LastSeenAt > DateTime.UtcNow.AddMinutes(-5) ? "Online" : "Offline") : "Inactive"
            };

            stats.Add(stat);
        }

        return Result<IReadOnlyList<DeviceStatsDto>>.Success(stats);
    }

    private static DeviceDto MapToDeviceDto(Device device) => new()
    {
        Id = device.Id,
        Name = device.Name,
        MacAddress = device.MacAddress,
        Location = device.Location,
        IsActive = device.IsActive,
        CreatedAt = device.CreatedAt,
        LastSeenAt = device.LastSeenAt,
        UserId = device.UserId
    };
}

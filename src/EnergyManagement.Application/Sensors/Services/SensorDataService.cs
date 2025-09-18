using EnergyManagement.Application.Devices.Domain;
using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Application.Sensors.Models.Dtos;
using EnergyManagement.Core.Common;
using Microsoft.Extensions.Logging;

namespace EnergyManagement.Application.Sensors.Services;
public class SensorDataService : ISensorDataService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ISensorReadingRepository _sensorRepository;
    private readonly ILogger<SensorDataService> _logger;

    public SensorDataService(
        IDeviceRepository deviceRepository,
        ISensorReadingRepository sensorRepository,
        ILogger<SensorDataService> logger)
    {
        _deviceRepository = deviceRepository;
        _sensorRepository = sensorRepository;
        _logger = logger;
    }

    public async Task<Result> ProcessSensorDataAsync(SensorDataDto sensorData)
    {
        try
        {
            // Validar dispositivo
            var device = await _deviceRepository.GetByMacAddressAsync(sensorData.MacAddress);
            if (device == null)
            {
                _logger.LogWarning("Tentativa de envio de dados de dispositivo não registrado: {MacAddress}", sensorData.MacAddress);
                return Result.Failure("Dispositivo não encontrado");
            }

            // Criar leitura do sensor
            var reading = new SensorReading
            {
                DeviceId = device.Id,
                Current = sensorData.Current,
                Voltage = sensorData.Voltage,
                Timestamp = sensorData.Timestamp.ToUniversalTime()
            };

            await _sensorRepository.AddAsync(reading);

            // Atualizar status do dispositivo
            await UpdateDeviceLastSeenAsync(device.Id);

            _logger.LogDebug("Dados processados do sensor {MacAddress}: {Power}W",
                sensorData.MacAddress, sensorData.Power);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar dados do sensor {MacAddress}", sensorData.MacAddress);
            return Result.Failure("Erro interno ao processar dados do sensor");
        }
    }

    public async Task<Result> ProcessBulkSensorDataAsync(BulkSensorDataDto bulkData)
    {
        try
        {
            // Validar dispositivo
            var device = await _deviceRepository.GetByMacAddressAsync(bulkData.MacAddress);
            if (device == null)
            {
                _logger.LogWarning("Tentativa de envio bulk de dispositivo não registrado: {MacAddress}", bulkData.MacAddress);
                return Result.Failure("Dispositivo não encontrado");
            }

            // Processar todas as leituras
            var readings = bulkData.Readings.Select(r => new SensorReading
            {
                DeviceId = device.Id,
                Current = r.Current,
                Voltage = r.Voltage
            }).ToList();

            await _sensorRepository.AddRangeAsync(readings);

            // Atualizar status do dispositivo
            await UpdateDeviceLastSeenAsync(device.Id);

            _logger.LogInformation("Processadas {Count} leituras do sensor {MacAddress}",
                readings.Count, bulkData.MacAddress);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar dados bulk do sensor {MacAddress}", bulkData.MacAddress);
            return Result.Failure("Erro interno ao processar dados bulk do sensor");
        }
    }

    public async Task<Result> ValidateDeviceAsync(string macAddress)
    {
        try
        {
            var device = await _deviceRepository.GetByMacAddressAsync(macAddress);

            if (device == null)
            {
                return Result.Failure("Dispositivo não encontrado");
            }

            if (!device.IsActive)
            {
                return Result.Failure("Dispositivo está inativo");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar dispositivo {MacAddress}", macAddress);
            return Result.Failure("Erro ao validar dispositivo");
        }
    }

    public async Task<Result> UpdateDeviceStatusAsync(string macAddress, bool isOnline)
    {
        try
        {
            var device = await _deviceRepository.GetByMacAddressAsync(macAddress);
            if (device == null)
            {
                return Result.Failure("Dispositivo não encontrado");
            }

            // Atualizar status (isso seria uma propriedade adicional na entidade Device)
            await UpdateDeviceLastSeenAsync(device.Id);

            _logger.LogDebug("Status do dispositivo {MacAddress} atualizado: {Status}",
                macAddress, isOnline ? "Online" : "Offline");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar status do dispositivo {MacAddress}", macAddress);
            return Result.Failure("Erro ao atualizar status do dispositivo");
        }
    }

    private async Task UpdateDeviceLastSeenAsync(int deviceId)
    {
        // Em uma implementação real, você atualizaria um campo LastSeen na entidade Device
        // Por ora, apenas logamos
        _logger.LogDebug("Device {DeviceId} last seen updated", deviceId);
    }
}

using EnergyManagement.Application.Sensors.Models.Dtos;
using EnergyManagement.Application.Sensors.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnergyManagement.WebApi.Controllers;

[ApiController]
[Route("api/sensor-data")]
public class SensorDataController : ControllerBase
{
    private readonly ISensorDataService _sensorDataService;
    private readonly ILogger<SensorDataController> _logger;

    public SensorDataController(ISensorDataService sensorDataService, ILogger<SensorDataController> logger)
    {
        _sensorDataService = sensorDataService;
        _logger = logger;
    }

    /// <summary>
    /// Recebe dados individuais do sensor ESP32 via MQTT/HTTP
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReceiveSensorData([FromBody] SensorDataDto sensorData)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _sensorDataService.ProcessSensorDataAsync(sensorData);

            if (result.IsFailure)
            {
                return result.Message.Contains("não encontrado")
                    ? NotFound(result.Message)
                    : BadRequest(result.Message);
            }

            return Ok(new { message = "Dados recebidos com sucesso", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao receber dados do sensor {MacAddress}", sensorData.MacAddress);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Recebe múltiplas leituras do sensor em uma única requisição (modo bulk)
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReceiveBulkSensorData([FromBody] BulkSensorDataDto bulkData)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _sensorDataService.ProcessBulkSensorDataAsync(bulkData);

            if (result.IsFailure)
            {
                return result.Message.Contains("não encontrado")
                    ? NotFound(result.Message)
                    : BadRequest(result.Message);
            }

            return Ok(new
            {
                message = "Dados bulk recebidos com sucesso",
                readingsCount = bulkData.Readings.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao receber dados bulk do sensor {MacAddress}", bulkData.MacAddress);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Valida se um dispositivo está autorizado a enviar dados
    /// </summary>
    [HttpGet("validate/{macAddress}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateDevice(string macAddress)
    {
        try
        {
            var result = await _sensorDataService.ValidateDeviceAsync(macAddress);

            if (result.IsFailure)
                return NotFound(result.Message);

            return Ok(new
            {
                message = "Dispositivo válido",
                macAddress = macAddress,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar dispositivo {MacAddress}", macAddress);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Endpoint para heartbeat/keepalive do dispositivo
    /// </summary>
    [HttpPost("heartbeat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeviceHeartbeat([FromBody] DeviceHeartbeatDto heartbeat)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _sensorDataService.UpdateDeviceStatusAsync(heartbeat.MacAddress, true);

            if (result.IsFailure)
                return BadRequest(result.Message);

            return Ok(new
            {
                message = "Heartbeat recebido",
                nextHeartbeat = DateTime.UtcNow.AddMinutes(5) // Próximo heartbeat em 5 minutos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar heartbeat do dispositivo {MacAddress}", heartbeat.MacAddress);
            return StatusCode(500, "Erro interno do servidor");
        }
    }
}
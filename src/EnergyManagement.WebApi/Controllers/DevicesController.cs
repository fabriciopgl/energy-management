using EnergyManagement.Application.Devices.Models.Dtos;
using EnergyManagement.Application.Devices.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnergyManagement.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class DevicesController(IDeviceApplicationService deviceService, ILogger<DevicesController> logger) : ControllerBase
{
    /// <summary>
    /// Obtém todos os dispositivos do usuário autenticado
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDevices()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await deviceService.GetAllByUserAsync(userId.Value);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter dispositivos do usuário");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém um dispositivo específico por ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDevice(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await deviceService.GetByIdAsync(userId.Value, id);

            if (result.IsFailure)
            {
                return result.Message.Contains("não encontrado")
                    ? NotFound(result.Message)
                    : Forbid(result.Message);
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter dispositivo {DeviceId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Cria um novo dispositivo
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await deviceService.CreateAsync(userId.Value, request);

            if (result.IsFailure)
            {
                return result.Message.Contains("já está em uso")
                    ? Conflict(result.Message)
                    : BadRequest(result.Message);
            }

            logger.LogInformation("Dispositivo criado: {DeviceName} pelo usuário {UserId}",
                request.Name, userId);

            return CreatedAtAction(nameof(GetDevice), new { id = result.Data!.Id }, result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar dispositivo para usuário {UserId}", GetCurrentUserId());
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Atualiza um dispositivo existente
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateDevice(int id, [FromBody] UpdateDeviceRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await deviceService.UpdateAsync(userId.Value, id, request);

            if (result.IsFailure)
            {
                return result.Message.Contains("não encontrado")
                    ? NotFound(result.Message)
                    : Forbid(result.Message);
            }

            logger.LogInformation("Dispositivo {DeviceId} atualizado pelo usuário {UserId}", id, userId);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar dispositivo {DeviceId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Remove um dispositivo
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await deviceService.DeleteAsync(userId.Value, id);

            if (result.IsFailure)
            {
                return result.Message.Contains("não encontrado")
                    ? NotFound(result.Message)
                    : Forbid(result.Message);
            }

            logger.LogInformation("Dispositivo {DeviceId} removido pelo usuário {UserId}", id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao remover dispositivo {DeviceId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém estatísticas dos dispositivos do usuário
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeviceStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await deviceService.GetStatsAsync(userId.Value);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter estatísticas dos dispositivos");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim is not null && int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

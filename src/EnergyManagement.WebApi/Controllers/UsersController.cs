using EnergyManagement.Application.Users.Domain;
using EnergyManagement.Application.Users.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnergyManagement.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserRepository userRepository, ILogger<UsersController> logger) : ControllerBase
{

    /// <summary>
    /// Obtém dados do usuário atual
    /// </summary>
    /// <returns>Dados completos do usuário logado</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await userRepository.GetByIdAsync(userId);
            if (user is null)
            {
                logger.LogWarning("Usuário não encontrado: {UserId}", userId);
                return NotFound();
            }

            var userDto = MapToUserDto(user);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter usuário atual");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Atualiza dados do usuário atual
    /// </summary>
    /// <param name="request">Dados para atualização</param>
    /// <returns>Dados atualizados do usuário</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await userRepository.GetByIdAsync(userId);
            if (user is null)
            {
                logger.LogWarning("Usuário não encontrado para atualização: {UserId}", userId);
                return NotFound();
            }

            // Atualiza apenas os campos permitidos
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            await userRepository.UpdateAsync(user);

            logger.LogInformation("Usuário atualizado com sucesso: {UserId}", userId);

            var userDto = MapToUserDto(user);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar usuário");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém estatísticas básicas do usuário
    /// </summary>
    /// <returns>Estatísticas do usuário (dispositivos, últimas leituras, etc.)</returns>
    [HttpGet("me/stats")]
    [ProducesResponseType(typeof(UserStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserStats()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await userRepository.GetByIdAsync(userId);
            if (user is null)
            {
                logger.LogWarning("Usuário não encontrado para estatísticas: {UserId}", userId);
                return NotFound();
            }

            var stats = new UserStatsDto
            {
                TotalDevices = user.Devices.Count,
                ActiveDevices = user.Devices.Count(d => d.IsActive),
                LastLogin = user.LastLoginAt,
                MemberSince = user.CreatedAt,
                TotalPreferences = user.Preferences.Count
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter estatísticas do usuário");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        IsActive = user.IsActive
    };
}

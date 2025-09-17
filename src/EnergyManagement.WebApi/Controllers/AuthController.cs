using EnergyManagement.Application.Users.Models.Dtos;
using EnergyManagement.Application.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnergyManagement.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthApplicationService authService, ILogger<AuthController> logger) : ControllerBase
{

    /// <summary>
    /// Registra um novo usuário no sistema
    /// </summary>
    /// <param name="request">Dados para registro do usuário</param>
    /// <returns>Token JWT e dados do usuário criado</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await authService.RegisterAsync(request);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha no registro do usuário: {Email}. Motivo: {Message}",
                    request.Email, result.Message);

                return result.Message.Contains("já está em uso")
                    ? Conflict(result.Data)
                    : BadRequest(result.Data);
            }

            logger.LogInformation("Usuário registrado com sucesso: {Email}", request.Email);
            return CreatedAtAction(nameof(GetProfile), new { }, result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno ao registrar usuário: {Email}", request.Email);
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = ["Tente novamente em alguns instantes"]
            });
        }
    }

    /// <summary>
    /// Autentica um usuário no sistema
    /// </summary>
    /// <param name="request">Credenciais de login</param>
    /// <returns>Token JWT e dados do usuário autenticado</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await authService.LoginAsync(request);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha no login do usuário: {Email}. Motivo: {Message}",
                    request.Email, result.Message);
                return Unauthorized(result.Data);
            }

            logger.LogInformation("Login realizado com sucesso: {Email}", request.Email);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno ao fazer login: {Email}", request.Email);
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = ["Tente novamente em alguns instantes"]
            });
        }
    }

    /// <summary>
    /// Atualiza o token JWT de um usuário autenticado
    /// </summary>
    /// <param name="request">Token atual para renovação</param>
    /// <returns>Novo token JWT</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await authService.RefreshTokenAsync(request.Token);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha ao renovar token. Motivo: {Message}", result.Message);
                return Unauthorized(result.Data);
            }

            logger.LogInformation("Token renovado com sucesso");
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro interno ao renovar token");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor",
                Errors = ["Tente novamente em alguns instantes"]
            });
        }
    }

    /// <summary>
    /// Obtém o perfil do usuário autenticado
    /// </summary>
    /// <returns>Dados do usuário logado</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var userDto = new UserDto
            {
                Id = userId,
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                FirstName = User.FindFirst("firstName")?.Value ?? string.Empty,
                LastName = User.FindFirst("lastName")?.Value ?? string.Empty,
                FullName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter perfil do usuário");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Valida se o token JWT é válido
    /// </summary>
    /// <returns>Status da validação do token</returns>
    [HttpPost("validate")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        return Ok(new { valid = true, message = "Token válido" });
    }
}

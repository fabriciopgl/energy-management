using EnergyManagement.Application.Users.Domain;
using EnergyManagement.Application.Users.Models.Dtos;
using EnergyManagement.Core.Common;
using Microsoft.AspNetCore.Identity;

namespace EnergyManagement.Application.Users.Services;
public class AuthApplicationService : IAuthApplicationService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;

    public AuthApplicationService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAuthService authService,
        IUserRepository userRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _authService = authService;
        _userRepository = userRepository;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var emailExists = await _userRepository.EmailExistsAsync(request.Email);
        if (emailExists)
            return Result<AuthResponseDto>.Failure(
                "Email já está em uso",
                ["Email já cadastrado no sistema"]);

        var user = new User
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Result<AuthResponseDto>.Failure(
                "Erro ao criar usuário",
                result.Errors.Select(e => e.Description).ToList());

        var token = await _authService.GenerateJwtTokenAsync(user);
        var response = new AuthResponseDto
        {
            Success = true,
            Message = "Usuário criado com sucesso",
            Token = token,
            User = MapToUserDto(user)
        };

        return Result<AuthResponseDto>.Success(response, "Usuário registrado com sucesso");
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Result<AuthResponseDto>.Failure(
                "Credenciais inválidas",
                ["Email ou senha incorretos"]);

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
            return Result<AuthResponseDto>.Failure(
                "Credenciais inválidas",
                ["Email ou senha incorretos"]);

        await _userRepository.UpdateLastLoginAsync(user.Id);
        var token = await _authService.GenerateJwtTokenAsync(user);

        var response = new AuthResponseDto
        {
            Success = true,
            Message = "Login realizado com sucesso",
            Token = token,
            User = MapToUserDto(user)
        };

        return Result<AuthResponseDto>.Success(response, "Login realizado com sucesso");
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string token)
    {
        var isValid = await _authService.ValidateTokenAsync(token);
        if (!isValid)
            return Result<AuthResponseDto>.Failure(
                "Token inválido",
                ["Token expirado ou inválido"]);

        var userId = await _authService.GetUserIdFromTokenAsync(token);
        if (userId is null)
            return Result<AuthResponseDto>.Failure(
                "Token inválido",
                ["Não foi possível extrair usuário do token"]);

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user is null || !user.IsActive)
            return Result<AuthResponseDto>.Failure(
                "Usuário não encontrado",
                ["Usuário não existe ou está inativo"]);

        var newToken = await _authService.GenerateJwtTokenAsync(user);
        var response = new AuthResponseDto
        {
            Success = true,
            Message = "Token atualizado com sucesso",
            Token = newToken,
            User = MapToUserDto(user)
        };

        return Result<AuthResponseDto>.Success(response, "Token atualizado com sucesso");
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

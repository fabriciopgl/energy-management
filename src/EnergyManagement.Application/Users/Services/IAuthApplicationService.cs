using EnergyManagement.Application.Users.Models.Dtos;
using EnergyManagement.Core.Common;

namespace EnergyManagement.Application.Users.Services;

public interface IAuthApplicationService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string token);
}
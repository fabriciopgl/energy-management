using System.ComponentModel.DataAnnotations;

namespace EnergyManagement.Application.Users.Models.Dtos;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "Token é obrigatório")]
    public string Token { get; set; } = string.Empty;
}
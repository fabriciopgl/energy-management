using System.ComponentModel.DataAnnotations;

namespace EnergyManagement.Application.Users.Models.Dtos;

public class UpdateUserRequestDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MinLength(2, ErrorMessage = "Nome deve ter pelo menos 2 caracteres")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sobrenome é obrigatório")]
    [MinLength(2, ErrorMessage = "Sobrenome deve ter pelo menos 2 caracteres")]
    [MaxLength(100, ErrorMessage = "Sobrenome deve ter no máximo 100 caracteres")]
    public string LastName { get; set; } = string.Empty;
}

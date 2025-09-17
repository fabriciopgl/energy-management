using System.ComponentModel.DataAnnotations;

namespace EnergyManagement.Application.Devices.Models.Dtos;

public class UpdateDeviceRequestDto
{
    [Required(ErrorMessage = "Nome do dispositivo é obrigatório")]
    [MinLength(2, ErrorMessage = "Nome deve ter pelo menos 2 caracteres")]
    [MaxLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Localização deve ter no máximo 200 caracteres")]
    public string Location { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
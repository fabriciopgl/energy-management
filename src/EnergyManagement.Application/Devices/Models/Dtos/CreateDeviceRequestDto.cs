using System.ComponentModel.DataAnnotations;

namespace EnergyManagement.Application.Devices.Models.Dtos;

public class CreateDeviceRequestDto
{
    [Required(ErrorMessage = "Nome do dispositivo é obrigatório")]
    [MinLength(2, ErrorMessage = "Nome deve ter pelo menos 2 caracteres")]
    [MaxLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "MAC Address é obrigatório")]
    [RegularExpression(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
        ErrorMessage = "MAC Address deve ter formato válido (XX:XX:XX:XX:XX:XX)")]
    public string MacAddress { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Localização deve ter no máximo 200 caracteres")]
    public string Location { get; set; } = string.Empty;
}

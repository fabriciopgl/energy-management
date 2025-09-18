using System.ComponentModel.DataAnnotations;

namespace EnergyManagement.Application.Analytics.Models.Requests;

public class ResolveAnomalyRequest
{
    [Required]
    public int AnomalyId { get; set; }
    public string? ResolutionNotes { get; set; }
}

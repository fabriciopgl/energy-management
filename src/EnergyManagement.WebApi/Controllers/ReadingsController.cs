using EnergyManagement.Application.Sensors.Domain;
using Microsoft.AspNetCore.Mvc;

namespace EnergyManagement.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadingsController(ISensorReadingRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int limit = 50)
    {
        var list = await repo.ListAsync(limit);
        return Ok(list);
    }
}

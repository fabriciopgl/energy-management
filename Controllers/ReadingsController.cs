using EnergyManagementApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EnergyManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadingsController(IReadingRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int limit = 50)
    {
        var list = await repo.ListAsync(limit);
        return Ok(list);
    }
}

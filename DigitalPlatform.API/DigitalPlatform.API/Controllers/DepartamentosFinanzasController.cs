using Microsoft.AspNetCore.Mvc;
using DigitalPlatform.Application.Interfaces;

namespace DigitalPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartamentosFinanzasController : ControllerBase
{
    private readonly IDepartamentoFinanzasService _service;

    public DepartamentosFinanzasController(IDepartamentoFinanzasService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var result = await _service.ObtenerTodosAsync();
        return Ok(result);
    }
}

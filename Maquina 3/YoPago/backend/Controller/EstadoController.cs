using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Data;

namespace Backend.Controllers;

/// <summary>Responde si la API y su base de datos están vivas. El front lo usa para mostrar "Conectado con el servidor".</summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class EstadoController : ControllerBase
{
    private readonly YoPagoContext _context;

    public EstadoController(YoPagoContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var baseDatos = await _context.Database.CanConnectAsync();
        var cuerpo = new { estado = baseDatos ? "ok" : "degradado", baseDatos, hora = DateTimeOffset.UtcNow };
        return baseDatos ? Ok(cuerpo) : StatusCode(503, cuerpo);
    }
}
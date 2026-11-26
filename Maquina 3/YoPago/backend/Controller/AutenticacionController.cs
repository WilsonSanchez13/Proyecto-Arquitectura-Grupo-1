using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Backend.Dtos;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public class AutenticacionController : ControllerBase
{
    private readonly AutenticacionService _servicio;

    public AutenticacionController(AutenticacionService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Ingreso con cédula y contraseña. Bloquea temporalmente tras varios intentos fallidos.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginRespuesta>> Login(LoginSolicitud solicitud) =>
        Ok(await _servicio.Login(solicitud));

    /// <summary>Envía por correo un enlace para crear una clave nueva. Responde igual exista o no la cédula.</summary>
    [HttpPost("recuperacion/solicitar")]
    public async Task<IActionResult> SolicitarRecuperacion(RecuperacionSolicitud solicitud)
    {
        await _servicio.SolicitarRecuperacion(solicitud);
        return NoContent();
    }

    [HttpPost("recuperacion/confirmar")]
    public async Task<IActionResult> ConfirmarRecuperacion(RecuperacionConfirmacion solicitud)
    {
        await _servicio.ConfirmarRecuperacion(solicitud);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Dtos;
using Backend.Extensiones;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComprasController : ControllerBase
{
    private readonly CompraService _servicio;

    public ComprasController(CompraService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Compras SSO pendientes por pago de la cédula autenticada.</summary>
    [HttpGet("pendientes")]
    public async Task<ActionResult<ComprasPendientesRespuesta>> Pendientes() =>
        Ok(await _servicio.ObtenerPendientes(User.ObtenerCedula()));

    /// <summary>Historial con contadores por estado. Filtros: estado (Pendiente|Pagada|Rechazada), meses (1-24), busqueda (N.º de compra).</summary>
    [HttpGet("historial")]
    public async Task<ActionResult<HistorialRespuesta>> Historial(
        [FromQuery] EstadoCompra? estado, [FromQuery] int meses = 6, [FromQuery] string? busqueda = null) =>
        Ok(await _servicio.ObtenerHistorial(User.ObtenerCedula(), estado, meses, busqueda));
}

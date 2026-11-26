using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Controllers;

/// <summary>
/// SOLO DESARROLLO: muestra lo que hay en la base para la página http://localhost:8000.
/// Responde 404 salvo que DatosDesarrollo:Habilitado sea true, y solo atiende peticiones hechas desde la misma máquina.
/// </summary>
[ApiController]
[Route("api/datos-desarrollo")]
[AllowAnonymous]
public class DatosDesarrolloController : ControllerBase
{
    private readonly YoPagoContext _context;
    private readonly IConfiguration _config;

    public DatosDesarrolloController(YoPagoContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var ip = HttpContext.Connection.RemoteIpAddress;
        var habilitado = _config.GetValue("DatosDesarrollo:Habilitado", false);
        if (!habilitado || ip is null || !System.Net.IPAddress.IsLoopback(ip)) return NotFound();

        var directorio = await _context.Directorio.AsNoTracking().OrderBy(u => u.Cedula)
            .Select(u => new { u.Cedula, u.Clave })
            .ToListAsync();

        var clientes = await _context.Clientes.AsNoTracking().OrderBy(c => c.CreadoEn)
            .Select(c => new { c.Cedula, c.Nombre, c.Correo, c.CreadoEn, c.BloqueadoHasta })
            .ToListAsync();

        var compras = await _context.Compras.AsNoTracking().OrderByDescending(c => c.RecibidaEn)
            .Select(c => new
            {
                c.NumeroCompra, c.Cliente.Cedula, c.Descripcion, c.CodigoSubasta, c.ValorOferta, c.Impuestos,
                c.Comision, c.ValorTotal, c.Estado, c.RecibidaEn, c.VenceEn, c.PagadaEn
            })
            .ToListAsync();

        var pagos = await _context.Pagos.AsNoTracking().OrderByDescending(p => p.CreadoEn)
            .Select(p => new
            {
                p.CodigoTransaccion, p.Compra.NumeroCompra, p.MedioPago, p.Banco, p.TipoCuenta, p.Valor,
                p.Estado, p.CodigoAcuseSso, p.CreadoEn
            })
            .ToListAsync();

        var mensajes = await _context.MensajesSalida.AsNoTracking().OrderByDescending(m => m.CreadoEn).Take(50)
            .Select(m => new { m.Tipo, m.CreadoEn, m.ProcesadoEn, m.Intentos, m.UltimoError })
            .ToListAsync();

        return Ok(new
        {
            usuarioPrueba = new { cedula = DatosPrueba.Cedula, clave = DatosPrueba.Clave },
            directorio, clientes, compras, pagos, mensajes
        });
    }
}

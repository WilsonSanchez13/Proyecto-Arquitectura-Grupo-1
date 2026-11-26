using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Dtos;
using Backend.Filtros;
using Backend.Services;

namespace Backend.Controllers;

/// <summary>
/// Endpoints que consume SSO. Se autentican con el encabezado X-Api-Key (no con el token de los clientes).
/// Contrato completo en docs/contrato-sso.md.
/// </summary>
[ApiController]
[Route("api/sso")]
[AllowAnonymous]
[ServiceFilter(typeof(ApiKeySsoFilter))]
public class SsoController : ControllerBase
{
    private readonly ComprasSsoService _servicio;

    public SsoController(ComprasSsoService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>SSO → YoPago (síncrono): registra una compra pendiente de pago. 201 si es nueva, 200 si ya existía.</summary>
    [HttpPost("compras")]
    public async Task<ActionResult<RespuestaCompraSso>> RecibirCompra(SolicitudCompraSso solicitud)
    {
        var respuesta = await _servicio.Recibir(solicitud);
        return respuesta.Creada
            ? CreatedAtAction(nameof(ConsultarCompra), new { numeroCompra = respuesta.NumeroCompra }, respuesta)
            : Ok(respuesta);
    }

    /// <summary>Polling: ¿el cliente ya pagó esta compra?</summary>
    [HttpGet("compras/{numeroCompra}")]
    public async Task<ActionResult<EstadoCompraSso>> ConsultarCompra(string numeroCompra) =>
        Ok(await _servicio.ConsultarEstado(numeroCompra));

    /// <summary>Polling por lotes: pagos confirmados desde una fecha (alternativa al webhook).</summary>
    [HttpGet("pagos")]
    public async Task<ActionResult<List<PagoConfirmadoWebhook>>> PagosConfirmados([FromQuery] DateTimeOffset? desde) =>
        Ok(await _servicio.ListarPagosConfirmados(desde));
}

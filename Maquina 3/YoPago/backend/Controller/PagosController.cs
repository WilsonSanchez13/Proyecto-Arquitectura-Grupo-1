using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Dtos;
using Backend.Extensiones;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagosController : ControllerBase
{
    private readonly PagoService _servicio;

    public PagosController(PagoService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Medios de pago, bancos y tipos de cuenta disponibles.</summary>
    [HttpGet("opciones")]
    public ActionResult<OpcionesPagoRespuesta> Opciones() => Ok(_servicio.ObtenerOpciones());

    /// <summary>Resumen de una compra pendiente para la pantalla de pago.</summary>
    [HttpGet("checkout/{numeroCompra}")]
    public async Task<ActionResult<CheckoutRespuesta>> Checkout(string numeroCompra) =>
        Ok(await _servicio.ObtenerCheckout(User.ObtenerCedula(), numeroCompra));

    /// <summary>Paga una compra. Un rechazo del banco responde 200 con resultado = Rechazado (no es un error del sistema).</summary>
    [HttpPost]
    public async Task<ActionResult<RespuestaPago>> Post(SolicitudPago solicitud) =>
        Ok(await _servicio.Pagar(User.ObtenerCedula(), solicitud));

    [HttpGet("{codigoTransaccion}")]
    public async Task<ActionResult<ComprobanteDto>> Get(string codigoTransaccion) =>
        Ok(await _servicio.ObtenerComprobante(User.ObtenerCedula(), codigoTransaccion));

    [HttpGet("{codigoTransaccion}/comprobante.pdf")]
    public async Task<IActionResult> ComprobantePdf(string codigoTransaccion)
    {
        var pdf = await _servicio.ObtenerComprobantePdf(User.ObtenerCedula(), codigoTransaccion);
        return File(pdf, "application/pdf", $"{codigoTransaccion}.pdf");
    }

    [HttpPost("{codigoTransaccion}/comprobante/correo")]
    public async Task<ActionResult<EnvioComprobanteRespuesta>> EnviarComprobante(string codigoTransaccion) =>
        Ok(await _servicio.EnviarComprobantePorCorreo(User.ObtenerCedula(), codigoTransaccion));
}

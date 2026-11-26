using Backend.Excepciones;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

/// <summary>Aplica a un pago la respuesta de SSO a su webhook (aceptado o rechazado).</summary>
public class AcuseSsoService
{
    private readonly IPagoRepository _pagos;
    private readonly TimeProvider _reloj;

    public AcuseSsoService(IPagoRepository pagos, TimeProvider reloj)
    {
        _pagos = pagos;
        _reloj = reloj;
    }

    public async Task Aplicar(int pagoId, bool aceptado, string? codigoAcuse, string? motivo)
    {
        var pago = await _pagos.GetByIdAsync(pagoId)
            ?? throw ReglaNegocioException.NoEncontrado("pago_no_encontrado", $"No existe el pago {pagoId}.");

        pago.AcuseSsoEn = _reloj.GetUtcNow();
        if (aceptado)
        {
            pago.CodigoAcuseSso = codigoAcuse;
        }
        else
        {
            pago.Estado = EstadoPago.RechazadoSso;
            pago.CodigoAcuseSso = codigoAcuse;
            pago.MensajeFallo = motivo;
            pago.Compra.Estado = EstadoCompra.Rechazada;
        }
        await _pagos.UpdateAsync(pago);
    }
}

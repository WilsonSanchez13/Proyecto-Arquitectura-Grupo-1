using Microsoft.Extensions.Options;
using Backend.Configuracion;
using Backend.Dtos;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Pasarela de pagos SIMULADA. Para producción se reemplaza el cuerpo de Cobrar por la integración
/// real con PSE / procesador de tarjetas; el resto del sistema no cambia.
/// </summary>
public class PasarelaPagoService
{
    private readonly PasarelaOpciones _opciones;

    public PasarelaPagoService(IOptions<PasarelaOpciones> opciones)
    {
        _opciones = opciones.Value;
    }

    public async Task<ResultadoCobro> Cobrar(SolicitudCobro solicitud)
    {
        await Task.Delay(_opciones.LatenciaMs);

        if (_opciones.CedulasRechazadas.Contains(solicitud.Cedula))
            return new ResultadoCobro(false, null, null, "51", "Fondos insuficientes");

        var ultimos4 = solicitud.CodigoTransaccion[^4..];
        var instrumento = solicitud.MedioPago switch
        {
            MedioPago.Pse => $"PSE · {solicitud.Banco} {solicitud.TipoCuenta} *{ultimos4}",
            MedioPago.Tarjeta => $"Tarjeta *{ultimos4}",
            MedioPago.Nequi => "Nequi / Bancolombia",
            _ => "Consignación / Efecty"
        };
        return new ResultadoCobro(true, $"SIM-{solicitud.CodigoTransaccion}", instrumento, null, null);
    }
}

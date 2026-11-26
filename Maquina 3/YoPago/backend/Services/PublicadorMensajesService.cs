using Backend.Dtos;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

/// <summary>
/// Envía al webhook de SSO los avisos guardados en la tabla mensajes_salida. Si SSO o la red caen,
/// el aviso permanece pendiente y se reintenta con espera creciente; nunca se pierde un pago confirmado.
/// </summary>
public class PublicadorMensajesService : BackgroundService
{
    private const int TamanoLote = 20;

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _reloj;
    private readonly ILogger<PublicadorMensajesService> _logger;

    public PublicadorMensajesService(IServiceScopeFactory scopes, TimeProvider reloj, ILogger<PublicadorMensajesService> logger)
    {
        _scopes = scopes;
        _reloj = reloj;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var espera = TimeSpan.FromSeconds(1);
            try
            {
                if (await EnviarLote() == TamanoLote) espera = TimeSpan.Zero;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Falló el ciclo de envío a SSO; se reintenta");
                espera = TimeSpan.FromSeconds(5);
            }

            try { await Task.Delay(espera, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task<int> EnviarLote()
    {
        using var scope = _scopes.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMensajeSalidaRepository>();
        var proxy = scope.ServiceProvider.GetRequiredService<ProxySsoService>();
        var acuses = scope.ServiceProvider.GetRequiredService<AcuseSsoService>();

        await using var transaccion = await repo.IniciarTransaccionAsync();
        var lote = await repo.GetPendientesAsync(_reloj.GetUtcNow(), TamanoLote);

        foreach (var mensaje in lote)
        {
            var resultado = await proxy.NotificarPago(mensaje.Contenido, $"mensaje-{mensaje.Id}");

            switch (resultado.Entrega)
            {
                case EstadoEntrega.Entregado:
                    mensaje.ProcesadoEn = _reloj.GetUtcNow();
                    mensaje.UltimoError = null;
                    if (mensaje.AgregadoId is int pagoId && !string.IsNullOrWhiteSpace(resultado.Acuse?.CodigoAcuse))
                        await acuses.Aplicar(pagoId, aceptado: true, resultado.Acuse!.CodigoAcuse, null);
                    break;

                case EstadoEntrega.Rechazado:
                    // SSO recibió el aviso pero lo rechazó: no tiene sentido reintentar.
                    mensaje.ProcesadoEn = _reloj.GetUtcNow();
                    mensaje.UltimoError = Recortar(resultado.Detalle);
                    if (mensaje.AgregadoId is int pagoRechazado)
                        await acuses.Aplicar(pagoRechazado, aceptado: false, resultado.Acuse?.CodigoAcuse ?? "RECHAZADO", resultado.Detalle);
                    _logger.LogWarning("SSO rechazó el aviso {Id}: {Detalle}", mensaje.Id, resultado.Detalle);
                    break;

                default:
                    mensaje.Intentos++;
                    mensaje.UltimoError = Recortar(resultado.Detalle);
                    var espera = TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, Math.Min(mensaje.Intentos, 8))));
                    mensaje.ProximoIntentoEn = _reloj.GetUtcNow().Add(espera);
                    _logger.LogWarning("No se pudo avisar a SSO ({Id}): {Detalle}. Intento {Intentos}, próximo en {Espera}",
                        mensaje.Id, resultado.Detalle, mensaje.Intentos, espera);
                    break;
            }
        }

        await repo.GuardarAsync();
        await transaccion.CommitAsync();
        return lote.Count;
    }

    private static string? Recortar(string? texto) =>
        texto is null ? null : (texto.Length > 450 ? texto[..450] : texto);
}

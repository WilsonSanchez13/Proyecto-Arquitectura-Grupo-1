using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Backend.Configuracion;
using Backend.Dtos;

namespace Backend.Services;

/// <summary>
/// Cliente HTTP hacia SSO (equivale al ProxyYoPago que SSO tiene hacia YoPago, pero en sentido contrario).
/// Envía el webhook de pago confirmado y traduce la respuesta de SSO en un resultado que el publicador entiende.
/// </summary>
public class ProxySsoService
{
    private readonly HttpClient _http;
    private readonly SsoOpciones _sso;

    public ProxySsoService(HttpClient http, IOptions<SsoOpciones> sso)
    {
        _http = http;
        _sso = sso.Value;
        _http.Timeout = TimeSpan.FromSeconds(_sso.TimeoutSegundos);
    }

    /// <summary>
    /// POST al webhook de SSO. 2xx = entregado (el cuerpo puede traer el acuse); 400/409/422 = SSO lo rechazó (definitivo);
    /// cualquier otra respuesta o un fallo de red = reintentar más tarde.
    /// </summary>
    public async Task<ResultadoWebhook> NotificarPago(string contenidoJson, string claveIdempotencia)
    {
        if (string.IsNullOrWhiteSpace(_sso.UrlWebhookPagos))
            return new ResultadoWebhook(EstadoEntrega.Reintentar, null, "Sso:UrlWebhookPagos no está configurada.");

        using var peticion = new HttpRequestMessage(HttpMethod.Post, _sso.UrlWebhookPagos)
        {
            Content = new StringContent(contenidoJson, Encoding.UTF8, "application/json")
        };
        peticion.Headers.Add("X-Api-Key", _sso.ApiKeySaliente);
        peticion.Headers.Add("Idempotency-Key", claveIdempotencia);

        try
        {
            using var respuesta = await _http.SendAsync(peticion);
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            var codigo = (int)respuesta.StatusCode;

            if (respuesta.IsSuccessStatusCode)
            {
                var acuse = LeerAcuse(cuerpo);
                return acuse?.Estado?.Trim().ToUpperInvariant() == "RECHAZADO"
                    ? new ResultadoWebhook(EstadoEntrega.Rechazado, acuse, acuse.Motivo)
                    : new ResultadoWebhook(EstadoEntrega.Entregado, acuse, null);
            }

            if (codigo is 400 or 409 or 422)
            {
                var acuse = LeerAcuse(cuerpo);
                var motivo = acuse?.Motivo ?? Recortar(cuerpo);
                return new ResultadoWebhook(EstadoEntrega.Rechazado,
                    new AcuseSsoRespuesta("RECHAZADO", acuse?.CodigoAcuse ?? $"HTTP-{codigo}", motivo), motivo);
            }

            return new ResultadoWebhook(EstadoEntrega.Reintentar, null, $"SSO respondió HTTP {codigo}");
        }
        catch (Exception ex) // red caída, timeout, DNS...
        {
            return new ResultadoWebhook(EstadoEntrega.Reintentar, null, ex.Message);
        }
    }

    private static AcuseSsoRespuesta? LeerAcuse(string cuerpo)
    {
        if (string.IsNullOrWhiteSpace(cuerpo)) return null;
        try { return JsonSerializer.Deserialize<AcuseSsoRespuesta>(cuerpo, JsonSso.Opciones); }
        catch (JsonException) { return null; }
    }

    private static string? Recortar(string texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : (texto.Length > 280 ? texto[..280] : texto);
}

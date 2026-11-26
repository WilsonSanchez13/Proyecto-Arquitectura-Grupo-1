namespace Backend.Models;

/// <summary>
/// Aviso pendiente de enviar al webhook de SSO (patrón Outbox): se guarda en la misma transacción que el pago
/// y un servicio en segundo plano lo envía con reintentos hasta que SSO lo reciba.
/// </summary>
public class MensajeSalida
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public int? AgregadoId { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ProximoIntentoEn { get; set; }
    public DateTimeOffset? ProcesadoEn { get; set; }
    public int Intentos { get; set; }
    public string? UltimoError { get; set; }
}

namespace Backend.Models;

public class Pago
{
    public int Id { get; set; }
    /// <summary>Número de transacción YoPago, p. ej. YP-88410293.</summary>
    public string CodigoTransaccion { get; set; } = string.Empty;
    public int CompraId { get; set; }
    public Compra Compra { get; set; } = null!;
    public MedioPago MedioPago { get; set; }
    public string? Banco { get; set; }
    public string? TipoCuenta { get; set; }
    public string CorreoComprobante { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public EstadoPago Estado { get; set; } = EstadoPago.Iniciado;
    public string? Instrumento { get; set; }
    public string? ReferenciaPasarela { get; set; }
    public string? CodigoFallo { get; set; }
    public string? MensajeFallo { get; set; }
    public string? CodigoAcuseSso { get; set; }
    public DateTimeOffset? AcuseSsoEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset? CompletadoEn { get; set; }
}

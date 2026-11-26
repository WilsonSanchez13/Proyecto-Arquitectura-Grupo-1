namespace Backend.Models;

/// <summary>Compra SSO por pagar, recibida desde SSO por cola. Los valores NO se modifican en YoPago.</summary>
public class Compra
{
    public int Id { get; set; }
    public string NumeroCompra { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public string Descripcion { get; set; } = string.Empty;
    public string? CodigoSubasta { get; set; }
    public decimal ValorOferta { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Comision { get; set; }
    public decimal ValorTotal { get; set; }
    public DateTimeOffset RecibidaEn { get; set; }
    public DateTimeOffset? VenceEn { get; set; }
    public DateTimeOffset? PagadaEn { get; set; }
    public DateTimeOffset SincronizadaEn { get; set; }
    public EstadoCompra Estado { get; set; } = EstadoCompra.Pendiente;

    public List<Pago> Pagos { get; set; } = new();
}

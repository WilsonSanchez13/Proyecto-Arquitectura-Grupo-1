using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Dtos;

public record MedioPagoOpcion(MedioPago Codigo, string Nombre, string Descripcion, bool Recomendado, bool RequiereBanco);

public record OpcionesPagoRespuesta(List<MedioPagoOpcion> Medios, List<string> Bancos, List<string> TiposCuenta);

public record CheckoutRespuesta(
    CompraDto Compra,
    string NombreCliente,
    string Cedula,
    string CorreoPredeterminado,
    decimal CostoTransaccion);

public class SolicitudPago
{
    [Required, StringLength(40)] public string NumeroCompra { get; set; } = string.Empty;
    [Required] public MedioPago MedioPago { get; set; }
    [StringLength(60)] public string? Banco { get; set; }
    [StringLength(30)] public string? TipoCuenta { get; set; }
    [Required, EmailAddress, StringLength(200)] public string CorreoComprobante { get; set; } = string.Empty;
    public bool AutorizaDebito { get; set; }
}

public enum ResultadoPago { Aprobado, Rechazado }

public record RespuestaPago(ResultadoPago Resultado, string? CodigoTransaccion, string Mensaje);

public record ComprobanteDto(
    string CodigoTransaccion,
    string NumeroCompra,
    string Descripcion,
    string Cedula,
    string NombreCliente,
    string Instrumento,
    decimal Valor,
    EstadoPago Estado,
    DateTimeOffset PagadoEn,
    DateTimeOffset? NotificadoSsoEn,
    string? CodigoAcuseSso,
    string? MensajeFallo,
    string CorreoComprobante);

public record EnvioComprobanteRespuesta(string EnviadoA);

// Contrato interno con la pasarela de pagos
public record SolicitudCobro(
    string CodigoTransaccion, decimal Valor, MedioPago MedioPago,
    string? Banco, string? TipoCuenta, string Cedula, string CorreoComprobante);

public record ResultadoCobro(bool Aprobado, string? Referencia, string? Instrumento, string? CodigoFallo, string? MensajeFallo);

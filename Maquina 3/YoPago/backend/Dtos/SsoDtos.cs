using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Models;

namespace Backend.Dtos;

// ===== SSO -> YoPago (POST /api/sso/compras) =====

/// <summary>Compra que SSO envía a YoPago. Obligatorios: Cedula, NumeroCompra y Valor.</summary>
public class SolicitudCompraSso
{
    [Required, StringLength(20, MinimumLength = 5)] public string Cedula { get; set; } = string.Empty;
    [Required, StringLength(40)] public string NumeroCompra { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999999999")] public decimal Valor { get; set; }

    public decimal? ValorOferta { get; set; }
    public decimal? Impuestos { get; set; }
    public decimal? Comision { get; set; }
    [StringLength(300)] public string? Descripcion { get; set; }
    [StringLength(40)] public string? CodigoSubasta { get; set; }
    [StringLength(200)] public string? NombreCliente { get; set; }
    [EmailAddress, StringLength(200)] public string? CorreoCliente { get; set; }
    public DateTimeOffset? RecibidaEn { get; set; }
    public DateTimeOffset? VenceEn { get; set; }
}

public record RespuestaCompraSso(
    string NumeroCompra,
    string Cedula,
    decimal Valor,
    EstadoCompra Estado,
    bool Creada,
    bool ClienteNuevo,
    string UrlPortal,
    string Mensaje);

/// <summary>Estado de una compra para el polling de SSO (GET /api/sso/compras/{numeroCompra}).</summary>
public record EstadoCompraSso(
    string NumeroCompra,
    string Cedula,
    decimal Valor,
    EstadoCompra Estado,
    bool Pagada,
    decimal? ValorPagado,
    DateTimeOffset? PagadaEn,
    string? CodigoTransaccion,
    string? CodigoAcuseSso);

// ===== YoPago -> SSO (webhook) =====

/// <summary>Cuerpo del POST que YoPago envía al webhook de SSO cuando un pago queda aprobado.</summary>
public record PagoConfirmadoWebhook(
    string CodigoTransaccion,
    string NumeroCompra,
    string Cedula,
    decimal ValorPagado,
    DateTimeOffset PagadoEn);

/// <summary>Cuerpo opcional con el que SSO responde al webhook. Estado: ACEPTADO | RECHAZADO.</summary>
public record AcuseSsoRespuesta(string? Estado, string? CodigoAcuse, string? Motivo);

public enum EstadoEntrega { Entregado, Rechazado, Reintentar }

public record ResultadoWebhook(EstadoEntrega Entrega, AcuseSsoRespuesta? Acuse, string? Detalle);

public static class TiposMensaje
{
    public const string PagoConfirmado = "pago.confirmado";
}

/// <summary>Opciones JSON únicas para hablar con SSO (camelCase, enums como texto).</summary>
public static class JsonSso
{
    public static readonly JsonSerializerOptions Opciones = Crear();

    private static JsonSerializerOptions Crear()
    {
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opciones.Converters.Add(new JsonStringEnumConverter());
        return opciones;
    }

    public static string Serializar<T>(T valor) => JsonSerializer.Serialize(valor, Opciones);
}

using Backend.Models;

namespace Backend.Dtos;

public record CompraDto(
    string NumeroCompra,
    string Descripcion,
    string? CodigoSubasta,
    decimal ValorOferta,
    decimal Impuestos,
    decimal Comision,
    decimal ValorTotal,
    DateTimeOffset RecibidaEn,
    DateTimeOffset? VenceEn,
    int? DiasParaVencer,
    bool PorVencer,
    EstadoCompra Estado,
    DateTimeOffset? PagadaEn,
    string? CodigoTransaccion,
    bool PuedePagar);

public record ComprasPendientesRespuesta(List<CompraDto> Items, DateTimeOffset? SincronizadaEn);

public record HistorialConteos(int Todas, int Pagadas, int Pendientes, int Rechazadas);

public record HistorialRespuesta(HistorialConteos Conteos, List<CompraDto> Items);

public record SincronizacionRespuesta(bool Solicitada);

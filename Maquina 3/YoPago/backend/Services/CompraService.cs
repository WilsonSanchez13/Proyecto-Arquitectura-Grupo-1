using Microsoft.Extensions.Options;
using Backend.Configuracion;
using Backend.Dtos;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public class CompraService
{
    private readonly ICompraRepository _repo;
    private readonly TimeProvider _reloj;
    private readonly AppOpciones _app;

    public CompraService(
        ICompraRepository repo,
        TimeProvider reloj,
        IOptions<AppOpciones> app)
    {
        _repo = repo;
        _reloj = reloj;
        _app = app.Value;
    }

    public async Task<ComprasPendientesRespuesta> ObtenerPendientes(string cedula)
    {
        var ahora = _reloj.GetUtcNow();
        var compras = await _repo.GetPendientesAsync(cedula);

        var items = compras
            .OrderBy(c => c.VenceEn == null)
            .ThenBy(c => c.VenceEn)
            .ThenByDescending(c => c.RecibidaEn)
            .Select(c => ConvertirADto(c, ahora))
            .ToList();

        return new ComprasPendientesRespuesta(items, await _repo.GetUltimaSincronizacionAsync(cedula));
    }

    public async Task<HistorialRespuesta> ObtenerHistorial(string cedula, EstadoCompra? estado, int meses, string? busqueda)
    {
        var ahora = _reloj.GetUtcNow();
        meses = Math.Clamp(meses, 1, 24);

        var compras = await _repo.GetHistorialAsync(cedula, ahora.AddMonths(-meses), busqueda);

        var conteos = new HistorialConteos(
            compras.Count,
            compras.Count(c => c.Estado == EstadoCompra.Pagada),
            compras.Count(c => c.Estado == EstadoCompra.Pendiente),
            compras.Count(c => c.Estado == EstadoCompra.Rechazada));

        var items = compras
            .Where(c => estado is null || c.Estado == estado)
            .OrderByDescending(c => c.PagadaEn ?? c.RecibidaEn)
            .Select(c => ConvertirADto(c, ahora))
            .ToList();

        return new HistorialRespuesta(conteos, items);
    }

    /// <summary>Convierte la entidad en lo que ve el front. Todos los cálculos (días, "por vencer", ¿se puede pagar?) se hacen aquí.</summary>
    public CompraDto ConvertirADto(Compra compra, DateTimeOffset ahora)
    {
        int? dias = compra.VenceEn is null ? null : (int)Math.Ceiling((compra.VenceEn.Value - ahora).TotalDays);
        var porVencer = compra.Estado == EstadoCompra.Pendiente && dias is not null && dias <= _app.DiasPorVencer;

        var codigoTransaccion = compra.Pagos
            .Where(p => p.Estado == EstadoPago.Aprobado || p.Estado == EstadoPago.RechazadoSso)
            .OrderByDescending(p => p.CreadoEn)
            .Select(p => p.CodigoTransaccion)
            .FirstOrDefault();

        return new CompraDto(
            compra.NumeroCompra, compra.Descripcion, compra.CodigoSubasta, compra.ValorOferta, compra.Impuestos,
            compra.Comision, compra.ValorTotal, compra.RecibidaEn, compra.VenceEn, dias, porVencer, compra.Estado,
            compra.PagadaEn, codigoTransaccion, compra.Estado == EstadoCompra.Pendiente);
    }
}

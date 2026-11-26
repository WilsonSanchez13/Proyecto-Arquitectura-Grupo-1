using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Repositories;

public class CompraRepository : ICompraRepository
{
    private readonly YoPagoContext _context;

    public CompraRepository(YoPagoContext context)
    {
        _context = context;
    }

    // Solo se cargan los pagos que sirven para mostrar el N.º de transacción (aprobados o rechazados por SSO).
    private IQueryable<Compra> ConPagosVisibles() =>
        _context.Compras.Include(c => c.Pagos.Where(p => p.Estado == EstadoPago.Aprobado || p.Estado == EstadoPago.RechazadoSso));

    public async Task<List<Compra>> GetPendientesAsync(string cedula) =>
        await ConPagosVisibles()
            .AsNoTracking()
            .Where(c => c.Cliente.Cedula == cedula && c.Estado == EstadoCompra.Pendiente)
            .ToListAsync();

    public async Task<List<Compra>> GetHistorialAsync(string cedula, DateTimeOffset desde, string? busqueda)
    {
        var consulta = ConPagosVisibles()
            .AsNoTracking()
            .Where(c => c.Cliente.Cedula == cedula && c.RecibidaEn >= desde);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToUpper();
            consulta = consulta.Where(c => c.NumeroCompra.ToUpper().Contains(termino));
        }

        return await consulta.ToListAsync();
    }

    public async Task<DateTimeOffset?> GetUltimaSincronizacionAsync(string cedula) =>
        await _context.Compras
            .Where(c => c.Cliente.Cedula == cedula)
            .MaxAsync(c => (DateTimeOffset?)c.SincronizadaEn);

    public async Task<Compra?> GetByNumeroAsync(string numeroCompra) =>
        await _context.Compras.FirstOrDefaultAsync(c => c.NumeroCompra == numeroCompra);

    public async Task<Compra?> GetParaSsoAsync(string numeroCompra) =>
        await _context.Compras
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Include(c => c.Pagos.Where(p => p.Estado == EstadoPago.Aprobado))
            .FirstOrDefaultAsync(c => c.NumeroCompra == numeroCompra);

    public async Task<Compra?> GetByNumeroYCedulaAsync(string numeroCompra, string cedula) =>
        await ConPagosVisibles()
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.NumeroCompra == numeroCompra && c.Cliente.Cedula == cedula);

    public async Task<Compra> AddAsync(Compra compra)
    {
        _context.Compras.Add(compra);
        await _context.SaveChangesAsync();
        return compra;
    }

    /// <summary>Guarda los cambios hechos a una compra que ya estaba cargada.</summary>
    public async Task UpdateAsync(Compra compra) => await _context.SaveChangesAsync();
}

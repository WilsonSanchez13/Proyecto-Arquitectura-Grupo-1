using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Repositories;

public class PagoRepository : IPagoRepository
{
    private readonly YoPagoContext _context;

    public PagoRepository(YoPagoContext context)
    {
        _context = context;
    }

    public async Task<List<Pago>> GetEnCursoAsync(int compraId) =>
        await _context.Pagos
            .Where(p => p.CompraId == compraId && p.Estado == EstadoPago.Iniciado)
            .ToListAsync();

    public async Task<bool> ExisteCodigoAsync(string codigoTransaccion) =>
        await _context.Pagos.AnyAsync(p => p.CodigoTransaccion == codigoTransaccion);

    public async Task<Pago?> GetComprobanteAsync(string codigoTransaccion, string cedula) =>
        await _context.Pagos
            .AsNoTracking()
            .Include(p => p.Compra).ThenInclude(c => c.Cliente)
            .FirstOrDefaultAsync(p => p.CodigoTransaccion == codigoTransaccion
                                      && p.Compra.Cliente.Cedula == cedula
                                      && (p.Estado == EstadoPago.Aprobado || p.Estado == EstadoPago.RechazadoSso));

    public async Task<Pago?> GetByIdAsync(int id) =>
        await _context.Pagos
            .Include(p => p.Compra)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Pago>> GetConfirmadosDesdeAsync(DateTimeOffset desde, int limite) =>
        await _context.Pagos
            .AsNoTracking()
            .Include(p => p.Compra).ThenInclude(c => c.Cliente)
            .Where(p => p.Estado == EstadoPago.Aprobado && p.CompletadoEn >= desde)
            .OrderBy(p => p.CompletadoEn)
            .Take(limite)
            .ToListAsync();

    public async Task<Pago> AddAsync(Pago pago)
    {
        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();
        return pago;
    }

    /// <summary>Guarda los cambios hechos a un pago que ya estaba cargado.</summary>
    public async Task UpdateAsync(Pago pago) => await _context.SaveChangesAsync();

    public async Task ConfirmarAsync(Pago pago, Compra compra, MensajeSalida mensaje)
    {
        _context.MensajesSalida.Add(mensaje);
        await _context.SaveChangesAsync(); // pago y compra ya están rastreados con sus cambios
    }
}

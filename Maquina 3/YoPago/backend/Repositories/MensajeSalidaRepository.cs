using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Backend.Data;
using Backend.Models;

namespace Backend.Repositories;

public class MensajeSalidaRepository : IMensajeSalidaRepository
{
    private readonly YoPagoContext _context;

    public MensajeSalidaRepository(YoPagoContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MensajeSalida mensaje)
    {
        _context.MensajesSalida.Add(mensaje);
        await _context.SaveChangesAsync();
    }

    public async Task<DateTimeOffset?> GetProcesadoEnAsync(int pagoId, string tipo) =>
        await _context.MensajesSalida
            .Where(m => m.AgregadoId == pagoId && m.Tipo == tipo)
            .Select(m => m.ProcesadoEn)
            .FirstOrDefaultAsync();

    public async Task<IDbContextTransaction> IniciarTransaccionAsync() =>
        await _context.Database.BeginTransactionAsync();

    // FOR UPDATE SKIP LOCKED permite correr varias instancias de la API sin duplicar envíos.
    public async Task<List<MensajeSalida>> GetPendientesAsync(DateTimeOffset ahora, int limite) =>
        await _context.MensajesSalida
            .FromSqlInterpolated($"""
                SELECT * FROM mensajes_salida
                WHERE procesado_en IS NULL AND proximo_intento_en <= {ahora}
                ORDER BY creado_en
                LIMIT {limite}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync();

    public async Task GuardarAsync() => await _context.SaveChangesAsync();
}

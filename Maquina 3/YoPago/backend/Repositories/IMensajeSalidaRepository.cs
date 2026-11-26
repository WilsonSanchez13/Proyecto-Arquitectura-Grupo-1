using Microsoft.EntityFrameworkCore.Storage;
using Backend.Models;

namespace Backend.Repositories;

public interface IMensajeSalidaRepository
{
    Task AddAsync(MensajeSalida mensaje);
    Task<DateTimeOffset?> GetProcesadoEnAsync(int pagoId, string tipo);

    Task<IDbContextTransaction> IniciarTransaccionAsync();
    /// <summary>Mensajes sin enviar. Debe llamarse dentro de una transacción: bloquea las filas para que dos instancias no envíen lo mismo.</summary>
    Task<List<MensajeSalida>> GetPendientesAsync(DateTimeOffset ahora, int limite);
    Task GuardarAsync();
}

using Backend.Models;

namespace Backend.Repositories;

public interface IPagoRepository
{
    Task<List<Pago>> GetEnCursoAsync(int compraId);
    Task<bool> ExisteCodigoAsync(string codigoTransaccion);
    Task<Pago?> GetComprobanteAsync(string codigoTransaccion, string cedula);
    Task<Pago?> GetByIdAsync(int id);
    Task<List<Pago>> GetConfirmadosDesdeAsync(DateTimeOffset desde, int limite);
    Task<Pago> AddAsync(Pago pago);
    Task UpdateAsync(Pago pago);

    /// <summary>
    /// Guarda en UNA sola transacción el pago aprobado, la compra pagada y el mensaje para SSO.
    /// Así nunca queda un pago cobrado sin su aviso a SSO.
    /// </summary>
    Task ConfirmarAsync(Pago pago, Compra compra, MensajeSalida mensaje);
}

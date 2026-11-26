using Backend.Models;

namespace Backend.Repositories;

public interface ICompraRepository
{
    Task<List<Compra>> GetPendientesAsync(string cedula);
    Task<List<Compra>> GetHistorialAsync(string cedula, DateTimeOffset desde, string? busqueda);
    Task<DateTimeOffset?> GetUltimaSincronizacionAsync(string cedula);
    Task<Compra?> GetByNumeroAsync(string numeroCompra);
    Task<Compra?> GetParaSsoAsync(string numeroCompra);
    Task<Compra?> GetByNumeroYCedulaAsync(string numeroCompra, string cedula);
    Task<Compra> AddAsync(Compra compra);
    Task UpdateAsync(Compra compra);
}

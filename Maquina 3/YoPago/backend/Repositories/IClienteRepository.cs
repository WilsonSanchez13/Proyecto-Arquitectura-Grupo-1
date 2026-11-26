using Backend.Models;

namespace Backend.Repositories;

public interface IClienteRepository
{
    Task<Cliente?> GetByCedulaAsync(string cedula);
    Task<Cliente> AddAsync(Cliente cliente);
    Task UpdateAsync(Cliente cliente);
}

using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly YoPagoContext _context;

    public ClienteRepository(YoPagoContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> GetByCedulaAsync(string cedula) =>
        await _context.Clientes.FirstOrDefaultAsync(c => c.Cedula == cedula);

    public async Task<Cliente> AddAsync(Cliente cliente)
    {
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return cliente;
    }

    /// <summary>Guarda los cambios hechos a un cliente que ya estaba cargado.</summary>
    public async Task UpdateAsync(Cliente cliente) => await _context.SaveChangesAsync();
}

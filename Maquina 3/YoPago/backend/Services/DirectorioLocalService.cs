using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Modo "Local" — "Active Directory" simulado de YoPago: una tabla cédula → clave (texto plano, sin cifrar).
/// Es el único lugar que conoce las claves; el resto del sistema solo pregunta "¿es válida?".
/// </summary>
public class DirectorioLocalService : IDirectorioActivo
{
    private readonly YoPagoContext _context;

    public DirectorioLocalService(YoPagoContext context)
    {
        _context = context;
    }

    public async Task<bool> Autenticar(string cedula, string clave)
    {
        var usuario = await _context.Directorio.AsNoTracking().FirstOrDefaultAsync(u => u.Cedula == cedula);
        return usuario is not null && usuario.Clave == clave;
    }

    /// <summary>Crea la entrada o, si ya existe, le cambia la clave.</summary>
    public async Task GuardarClave(string cedula, string clave)
    {
        var usuario = await _context.Directorio.FirstOrDefaultAsync(u => u.Cedula == cedula);
        if (usuario is null) _context.Directorio.Add(new UsuarioDirectorio { Cedula = cedula, Clave = clave });
        else usuario.Clave = clave;
        await _context.SaveChangesAsync();
    }
}

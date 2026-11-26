namespace Backend.Services;

/// <summary>
/// Directorio donde viven las credenciales de los clientes. Dos implementaciones, elegidas con Directorio:Modo:
/// "Local" (tabla directorio_activo, claves en texto plano) y "Ldap" (un Active Directory / servidor LDAP real).
/// </summary>
public interface IDirectorioActivo
{
    /// <summary>¿La cédula existe en el directorio y la clave es la correcta?</summary>
    Task<bool> Autenticar(string cedula, string clave);

    /// <summary>Crea el usuario o, si ya existe, le cambia la clave.</summary>
    Task GuardarClave(string cedula, string clave);
}

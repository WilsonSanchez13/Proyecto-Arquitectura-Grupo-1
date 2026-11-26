using System.Text;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;
using Backend.Configuracion;
using Backend.Excepciones;

namespace Backend.Services;

/// <summary>
/// Modo "Ldap": las credenciales viven en un Active Directory (o cualquier servidor LDAP) y YoPago no guarda claves.
/// Autenticar = intentar un bind LDAP con el usuario y su clave; si el servidor lo acepta, la clave es válida.
/// Guardar clave usa una cuenta de servicio (UsuarioAdmin) para crear el usuario o cambiarle la clave.
/// </summary>
public class DirectorioLdapService : IDirectorioActivo
{
    private readonly LdapOpciones _ldap;
    private readonly ILogger<DirectorioLdapService> _logger;

    public DirectorioLdapService(IOptions<LdapOpciones> ldap, ILogger<DirectorioLdapService> logger)
    {
        _ldap = ldap.Value;
        _logger = logger;
    }

    public async Task<bool> Autenticar(string cedula, string clave)
    {
        // Un bind simple con clave vacía el servidor lo toma como acceso anónimo y "funciona": hay que rechazarlo aquí.
        if (!EsCedula(cedula) || string.IsNullOrEmpty(clave)) return false;

        try
        {
            using var conexion = await Conectar();
            await conexion.BindAsync(_ldap.PlantillaUsuario.Replace("{cedula}", cedula), clave);
            return conexion.Bound;
        }
        catch (LdapException ex) when (ex.ResultCode is LdapException.InvalidCredentials or LdapException.NoSuchObject)
        {
            return false; // usuario o clave incorrectos
        }
        catch (Exception ex)
        {
            // Red caída, servidor apagado, certificado inválido...: no es un "clave incorrecta" y no debe sumar intentos fallidos.
            _logger.LogError(ex, "No se pudo consultar el directorio LDAP {Host}:{Puerto}", _ldap.Host, _ldap.Puerto);
            throw ReglaNegocioException.PasarelaNoDisponible("directorio_no_disponible",
                "No pudimos validar tus credenciales en este momento. Intenta de nuevo en unos minutos.");
        }
    }

    public async Task GuardarClave(string cedula, string clave)
    {
        if (!EsCedula(cedula)) throw new ArgumentException("Cédula inválida", nameof(cedula));
        if (string.IsNullOrEmpty(_ldap.UsuarioAdmin))
            throw new InvalidOperationException("Ldap:UsuarioAdmin y Ldap:ClaveAdmin son necesarios para crear usuarios o cambiar claves.");

        try
        {
            using var conexion = await Conectar();
            await conexion.BindAsync(_ldap.UsuarioAdmin, _ldap.ClaveAdmin);

            var atributoClave = CrearAtributoClave(clave);
            var dn = await BuscarDn(conexion, cedula);
            if (dn is not null)
            {
                await conexion.ModifyAsync(dn, new LdapModification(LdapModification.Replace, atributoClave));
                return;
            }

            // Usuario nuevo (estilo OpenLDAP/inetOrgPerson). En un AD real los usuarios suele crearlos TI: ver README del directorio.
            var atributos = new LdapAttributeSet
            {
                new LdapAttribute("objectClass", new[] { "top", "person", "organizationalPerson", "inetOrgPerson" }),
                new LdapAttribute("cn", cedula),
                new LdapAttribute("sn", cedula),
                new LdapAttribute(_ldap.AtributoLogin, cedula),
                atributoClave
            };
            await conexion.AddAsync(new LdapEntry($"{_ldap.AtributoLogin}={cedula},{_ldap.BaseUsuarios}", atributos));
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "No se pudo guardar la clave de {Cedula} en el directorio LDAP", cedula);
            throw ReglaNegocioException.PasarelaNoDisponible("directorio_no_disponible",
                "No pudimos actualizar tu clave en este momento. Intenta de nuevo en unos minutos.");
        }
    }

    // ---------------- ayudas ----------------

    private async Task<LdapConnection> Conectar()
    {
        var opciones = new LdapConnectionOptions();
        if (_ldap.UsarSsl) opciones.UseSsl();

        var conexion = new LdapConnection(opciones) { ConnectionTimeout = _ldap.TimeoutSegundos * 1000 };
        try
        {
            await conexion.ConnectAsync(_ldap.Host, _ldap.Puerto);
            if (_ldap.UsarStartTls) await conexion.StartTlsAsync();
            return conexion;
        }
        catch
        {
            conexion.Dispose();
            throw;
        }
    }

    private async Task<string?> BuscarDn(LdapConnection conexion, string cedula)
    {
        // La cédula ya viene validada como solo dígitos, así que no hay inyección LDAP en el filtro.
        var resultados = await conexion.SearchAsync(_ldap.BaseUsuarios, LdapConnection.ScopeSub,
            $"({_ldap.AtributoLogin}={cedula})", new[] { "1.1" }, false);
        while (await resultados.HasMoreAsync())
        {
            var entrada = await resultados.NextAsync();
            if (entrada is not null) return entrada.Dn;
        }
        return null;
    }

    private LdapAttribute CrearAtributoClave(string clave) =>
        _ldap.AtributoClave.Equals("unicodePwd", StringComparison.OrdinalIgnoreCase)
            ? new LdapAttribute("unicodePwd", Encoding.Unicode.GetBytes($"\"{clave}\"")) // AD: comillas + UTF-16LE, solo por LDAPS
            : new LdapAttribute(_ldap.AtributoClave, clave);

    private static bool EsCedula(string valor) => valor.Length is >= 5 and <= 12 && valor.All(char.IsAsciiDigit);
}

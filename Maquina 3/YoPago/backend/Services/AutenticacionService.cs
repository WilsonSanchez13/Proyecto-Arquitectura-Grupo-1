using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Backend.Configuracion;
using Backend.Dtos;
using Backend.Excepciones;
using Backend.Repositories;

namespace Backend.Services;

public class AutenticacionService
{
    private readonly IClienteRepository _clientes;
    private readonly IDirectorioActivo _directorio;
    private readonly TokenService _tokens;
    private readonly CorreoService _correo;
    private readonly TimeProvider _reloj;
    private readonly SeguridadOpciones _seguridad;
    private readonly AppOpciones _app;
    private readonly ILogger<AutenticacionService> _logger;

    public AutenticacionService(
        IClienteRepository clientes,
        IDirectorioActivo directorio,
        TokenService tokens,
        CorreoService correo,
        TimeProvider reloj,
        IOptions<SeguridadOpciones> seguridad,
        IOptions<AppOpciones> app,
        ILogger<AutenticacionService> logger)
    {
        _clientes = clientes;
        _directorio = directorio;
        _tokens = tokens;
        _correo = correo;
        _reloj = reloj;
        _seguridad = seguridad.Value;
        _app = app.Value;
        _logger = logger;
    }

    public async Task<LoginRespuesta> Login(LoginSolicitud solicitud)
    {
        var cedula = NormalizarCedula(solicitud.Cedula);
        var cliente = cedula is null ? null : await _clientes.GetByCedulaAsync(cedula);

        if (cliente is null)
        {
            throw ReglaNegocioException.NoAutorizado("credenciales_invalidas", "Credenciales inválidas.");
        }

        var ahora = _reloj.GetUtcNow();
        if (cliente.BloqueadoHasta is not null && cliente.BloqueadoHasta > ahora)
        {
            var minutos = (int)Math.Ceiling((cliente.BloqueadoHasta.Value - ahora).TotalMinutes);
            throw ReglaNegocioException.Bloqueado("usuario_bloqueado",
                $"Usuario bloqueado temporalmente. Intenta de nuevo en {minutos} min o recupera tu contraseña.");
        }

        if (!await _directorio.Autenticar(cliente.Cedula, solicitud.Clave))
        {
            cliente.IntentosFallidos++;
            var restantes = _seguridad.MaxIntentosFallidos - cliente.IntentosFallidos;
            if (restantes <= 0)
            {
                cliente.BloqueadoHasta = ahora.AddMinutes(_seguridad.MinutosBloqueo);
                cliente.IntentosFallidos = 0;
                restantes = 0;
            }
            await _clientes.UpdateAsync(cliente);

            var mensaje = restantes > 0
                ? $"Credenciales inválidas. Te quedan {restantes} intentos antes del bloqueo temporal."
                : "Credenciales inválidas. Tu usuario quedó bloqueado temporalmente.";
            var error = ReglaNegocioException.NoAutorizado("credenciales_invalidas", mensaje);
            error.Extensiones["intentosRestantes"] = restantes;
            throw error;
        }

        cliente.IntentosFallidos = 0;
        cliente.BloqueadoHasta = null;
        await _clientes.UpdateAsync(cliente);

        var (token, vence) = _tokens.Crear(cliente);
        return new LoginRespuesta(token, vence, new UsuarioSesion(cliente.Cedula, cliente.Nombre));
    }

    /// <summary>Siempre responde igual exista o no la cédula, para no revelar qué clientes tiene YoPago.</summary>
    public async Task SolicitarRecuperacion(RecuperacionSolicitud solicitud)
    {
        var cedula = NormalizarCedula(solicitud.Cedula);
        if (cedula is null) return;

        var cliente = await _clientes.GetByCedulaAsync(cedula);
        if (cliente is null) return;

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        cliente.TokenRecuperacionHash = HashSha256(token);
        cliente.TokenRecuperacionVence = _reloj.GetUtcNow().AddMinutes(_seguridad.MinutosTokenRecuperacion);
        await _clientes.UpdateAsync(cliente);

        var enlace = $"{_app.UrlFront.TrimEnd('/')}/restablecer?cedula={cedula}&token={token}";
        try
        {
            await _correo.Enviar(
                cliente.Correo,
                "Recupera tu acceso a YoPago",
                "<p>Recibimos una solicitud para restablecer tu clave de YoPago.</p>" +
                $"<p><a href=\"{enlace}\">Crear una clave nueva</a> (vence en {_seguridad.MinutosTokenRecuperacion} minutos).</p>" +
                "<p>Si no fuiste tú, ignora este mensaje.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo enviar el correo de recuperación a la cédula {Cedula}: {Error}", cedula, ex.Message);
        }
    }

    public async Task ConfirmarRecuperacion(RecuperacionConfirmacion solicitud)
    {
        ValidarPoliticaClave(solicitud.ClaveNueva);

        var cedula = NormalizarCedula(solicitud.Cedula)
            ?? throw ReglaNegocioException.Validacion("cedula_invalida", "El número de cédula no es válido.");
        var cliente = await _clientes.GetByCedulaAsync(cedula);

        var valido = cliente?.TokenRecuperacionHash is not null
            && cliente.TokenRecuperacionVence > _reloj.GetUtcNow()
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(cliente.TokenRecuperacionHash),
                Encoding.UTF8.GetBytes(HashSha256(solicitud.Token)));

        if (!valido)
            throw ReglaNegocioException.Validacion("token_recuperacion_invalido",
                "El enlace no es válido o ya venció. Solicita uno nuevo.");

        await _directorio.GuardarClave(cliente!.Cedula, solicitud.ClaveNueva);
        cliente.TokenRecuperacionHash = null;
        cliente.TokenRecuperacionVence = null;
        cliente.IntentosFallidos = 0;
        cliente.BloqueadoHasta = null;
        await _clientes.UpdateAsync(cliente);
    }

    /// <summary>Deja solo dígitos ("1.032.456.789" → "1032456789"). Devuelve null si no es plausible.</summary>
    public static string? NormalizarCedula(string? cedula)
    {
        var digitos = new string((cedula ?? string.Empty).Where(char.IsDigit).ToArray());
        return digitos.Length is >= 5 and <= 12 ? digitos : null;
    }

    private static void ValidarPoliticaClave(string clave)
    {
        var cumple = clave.Length >= 10 && clave.Any(char.IsUpper) && clave.Any(char.IsLower) && clave.Any(char.IsDigit);
        if (!cumple)
            throw ReglaNegocioException.Validacion("clave_debil",
                "La clave debe tener mínimo 10 caracteres, con mayúscula, minúscula y número.");
    }

    private static string HashSha256(string valor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor))).ToLowerInvariant();
}

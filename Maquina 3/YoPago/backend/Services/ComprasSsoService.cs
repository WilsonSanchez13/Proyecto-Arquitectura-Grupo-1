using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Backend.Configuracion;
using Backend.Dtos;
using Backend.Excepciones;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

/// <summary>Atiende las llamadas REST de SSO: recibir una compra y consultar el estado de pago (polling).</summary>
public class ComprasSsoService
{
    private readonly IClienteRepository _clientes;
    private readonly ICompraRepository _compras;
    private readonly IPagoRepository _pagos;
    private readonly IDirectorioActivo _directorio;
    private readonly CorreoService _correo;
    private readonly TimeProvider _reloj;
    private readonly AppOpciones _app;
    private readonly ILogger<ComprasSsoService> _logger;

    public ComprasSsoService(
        IClienteRepository clientes,
        ICompraRepository compras,
        IPagoRepository pagos,
        IDirectorioActivo directorio,
        CorreoService correo,
        TimeProvider reloj,
        IOptions<AppOpciones> app,
        ILogger<ComprasSsoService> logger)
    {
        _clientes = clientes;
        _compras = compras;
        _pagos = pagos;
        _directorio = directorio;
        _correo = correo;
        _reloj = reloj;
        _app = app.Value;
        _logger = logger;
    }

    /// <summary>Es idempotente: si SSO envía dos veces la misma compra no se duplica nada.</summary>
    public async Task<RespuestaCompraSso> Recibir(SolicitudCompraSso solicitud)
    {
        var cedula = AutenticacionService.NormalizarCedula(solicitud.Cedula)
            ?? throw ReglaNegocioException.Validacion("cedula_invalida", "El número de cédula no es válido.");

        var impuestos = solicitud.Impuestos ?? 0m;
        var comision = solicitud.Comision ?? 0m;
        var oferta = solicitud.ValorOferta ?? solicitud.Valor - impuestos - comision;
        if (oferta + impuestos + comision != solicitud.Valor)
            throw ReglaNegocioException.Validacion("valores_inconsistentes",
                "ValorOferta + Impuestos + Comision debe ser igual a Valor.");

        var ahora = _reloj.GetUtcNow();
        var numeroCompra = solicitud.NumeroCompra.Trim();
        string? claveInicial = null;

        var cliente = await _clientes.GetByCedulaAsync(cedula);
        if (cliente is null)
        {
            if (string.IsNullOrWhiteSpace(solicitud.CorreoCliente))
                throw ReglaNegocioException.Validacion("cliente_sin_correo",
                    "La cédula es nueva en YoPago: envía correoCliente (y nombreCliente) para crear el usuario.");

            claveInicial = GenerarClave();
            cliente = await _clientes.AddAsync(new Cliente
            {
                Cedula = cedula,
                Nombre = string.IsNullOrWhiteSpace(solicitud.NombreCliente) ? "Cliente SSO" : solicitud.NombreCliente.Trim(),
                Correo = solicitud.CorreoCliente.Trim(),
                CreadoEn = ahora
            });
            await _directorio.GuardarClave(cedula, claveInicial);
        }

        var compra = await _compras.GetByNumeroAsync(numeroCompra);
        bool creada;
        if (compra is null)
        {
            creada = true;
            compra = await _compras.AddAsync(new Compra
            {
                NumeroCompra = numeroCompra,
                ClienteId = cliente.Id,
                Descripcion = solicitud.Descripcion?.Trim() ?? "Bien adjudicado en subasta SSO",
                CodigoSubasta = solicitud.CodigoSubasta,
                ValorOferta = oferta,
                Impuestos = impuestos,
                Comision = comision,
                ValorTotal = solicitud.Valor,
                RecibidaEn = solicitud.RecibidaEn ?? ahora,
                VenceEn = solicitud.VenceEn,
                SincronizadaEn = ahora
            });
        }
        else
        {
            creada = false;
            if (compra.ClienteId != cliente.Id)
                throw ReglaNegocioException.Conflicto("compra_de_otro_cliente",
                    $"La compra {numeroCompra} ya está asociada a otra cédula.");

            // Una compra ya pagada o rechazada no se vuelve a abrir por un reenvío de SSO.
            if (compra.Estado == EstadoCompra.Pendiente)
            {
                compra.Descripcion = solicitud.Descripcion?.Trim() ?? compra.Descripcion;
                compra.CodigoSubasta = solicitud.CodigoSubasta ?? compra.CodigoSubasta;
                compra.ValorOferta = oferta;
                compra.Impuestos = impuestos;
                compra.Comision = comision;
                compra.ValorTotal = solicitud.Valor;
                compra.VenceEn = solicitud.VenceEn ?? compra.VenceEn;
            }
            compra.SincronizadaEn = ahora;
            await _compras.UpdateAsync(compra);
        }

        _logger.LogInformation("Compra {Compra} recibida de SSO para la cédula {Cedula} ({Resultado})",
            numeroCompra, cedula, creada ? "creada" : "actualizada");

        if (claveInicial is not null) await EnviarClaveInicial(cliente, claveInicial);

        var mensaje = compra.Estado switch
        {
            EstadoCompra.Pagada => "La compra ya fue pagada en YoPago.",
            EstadoCompra.Rechazada => "SSO rechazó la notificación de pago de esta compra.",
            _ => creada ? "Compra registrada; el cliente ya puede pagarla en YoPago." : "Compra actualizada."
        };

        return new RespuestaCompraSso(compra.NumeroCompra, cedula, compra.ValorTotal, compra.Estado, creada,
            claveInicial is not null, _app.UrlFront.TrimEnd('/') + "/ingresar", mensaje);
    }

    /// <summary>Polling: SSO pregunta si el cliente ya pagó una compra.</summary>
    public async Task<EstadoCompraSso> ConsultarEstado(string numeroCompra)
    {
        var compra = await _compras.GetParaSsoAsync(numeroCompra)
            ?? throw ReglaNegocioException.NoEncontrado("compra_no_encontrada", $"No existe la compra {numeroCompra} en YoPago.");

        var pago = compra.Pagos
            .Where(p => p.Estado == EstadoPago.Aprobado)
            .OrderByDescending(p => p.CreadoEn)
            .FirstOrDefault();

        return new EstadoCompraSso(compra.NumeroCompra, compra.Cliente.Cedula, compra.ValorTotal, compra.Estado,
            pago is not null, pago?.Valor, compra.PagadaEn, pago?.CodigoTransaccion, pago?.CodigoAcuseSso);
    }

    /// <summary>Polling por lotes: pagos confirmados desde una fecha (para SSO si prefiere no usar el webhook).</summary>
    public async Task<List<PagoConfirmadoWebhook>> ListarPagosConfirmados(DateTimeOffset? desde)
    {
        var pagos = await _pagos.GetConfirmadosDesdeAsync(desde ?? DateTimeOffset.UnixEpoch, 500);
        return pagos
            .Select(p => new PagoConfirmadoWebhook(p.CodigoTransaccion, p.Compra.NumeroCompra, p.Compra.Cliente.Cedula,
                p.Valor, p.CompletadoEn ?? p.CreadoEn))
            .ToList();
    }

    private async Task EnviarClaveInicial(Cliente cliente, string clave)
    {
        try
        {
            var url = _app.UrlFront.TrimEnd('/') + "/ingresar";
            await _correo.Enviar(
                cliente.Correo,
                "Tu clave inicial de YoPago",
                $"<p>Hola {System.Net.WebUtility.HtmlEncode(cliente.Nombre)},</p>" +
                $"<p>SSO te envió una compra para pagar. Ingresa a <a href=\"{url}\">YoPago</a> con tu cédula y esta clave inicial:</p>" +
                $"<p><strong>{clave}</strong></p>" +
                "<p>Nunca te pediremos claves por teléfono o correo.</p>");
        }
        catch (Exception ex)
        {
            // La compra ya quedó guardada; si el correo falla el cliente puede usar "¿Olvidaste tu contraseña?".
            _logger.LogWarning("No se pudo enviar la clave inicial al cliente {Cedula}: {Error}", cliente.Cedula, ex.Message);
        }
    }

    private static string GenerarClave()
    {
        const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        return new string(Enumerable.Range(0, 12).Select(_ => alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)]).ToArray());
    }
}

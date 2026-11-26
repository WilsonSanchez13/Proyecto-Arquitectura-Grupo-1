using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Backend.Configuracion;
using Backend.Dtos;
using Backend.Excepciones;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public class PagoService
{
    private readonly IPagoRepository _pagos;
    private readonly ICompraRepository _compras;
    private readonly IMensajeSalidaRepository _mensajes;
    private readonly CompraService _compraService;
    private readonly PasarelaPagoService _pasarela;
    private readonly ComprobantePdfService _pdf;
    private readonly CorreoService _correo;
    private readonly TimeProvider _reloj;
    private readonly AppOpciones _app;
    private readonly ILogger<PagoService> _logger;

    public PagoService(
        IPagoRepository pagos,
        ICompraRepository compras,
        IMensajeSalidaRepository mensajes,
        CompraService compraService,
        PasarelaPagoService pasarela,
        ComprobantePdfService pdf,
        CorreoService correo,
        TimeProvider reloj,
        IOptions<AppOpciones> app,
        ILogger<PagoService> logger)
    {
        _pagos = pagos;
        _compras = compras;
        _mensajes = mensajes;
        _compraService = compraService;
        _pasarela = pasarela;
        _pdf = pdf;
        _correo = correo;
        _reloj = reloj;
        _app = app.Value;
        _logger = logger;
    }

    public OpcionesPagoRespuesta ObtenerOpciones() =>
        new(CatalogoPagos.Medios, CatalogoPagos.Bancos, CatalogoPagos.TiposCuenta);

    /// <summary>Datos para la pantalla "Elige cómo pagar". Solo compras pendientes de este cliente.</summary>
    public async Task<CheckoutRespuesta> ObtenerCheckout(string cedula, string numeroCompra)
    {
        var compra = await _compras.GetByNumeroYCedulaAsync(numeroCompra, cedula)
            ?? throw ReglaNegocioException.NoEncontrado("compra_no_encontrada", "No encontramos esa compra asociada a tu cédula.");
        ValidarPagable(compra.Estado);

        var dto = _compraService.ConvertirADto(compra, _reloj.GetUtcNow());
        return new CheckoutRespuesta(dto, compra.Cliente.Nombre, compra.Cliente.Cedula, compra.Cliente.Correo, 0m);
    }

    public async Task<RespuestaPago> Pagar(string cedula, SolicitudPago solicitud)
    {
        if (!solicitud.AutorizaDebito)
            throw ReglaNegocioException.Validacion("autorizacion_requerida",
                "Debes autorizar el débito y el envío de la confirmación a SSO.");
        ValidarMedio(solicitud);

        var ahora = _reloj.GetUtcNow();
        var compra = await _compras.GetByNumeroYCedulaAsync(solicitud.NumeroCompra, cedula)
            ?? throw ReglaNegocioException.NoEncontrado("compra_no_encontrada", "No encontramos esa compra asociada a tu cédula.");
        ValidarPagable(compra.Estado);

        // 1) Cerrar intentos colgados y evitar dos cobros simultáneos de la misma compra.
        var limite = ahora.AddMinutes(-_app.MinutosIntentoPago);
        var enCurso = await _pagos.GetEnCursoAsync(compra.Id);
        var vencidos = enCurso.Where(p => p.CreadoEn < limite).ToList();
        foreach (var vencido in vencidos)
        {
            vencido.Estado = EstadoPago.Vencido;
            vencido.CompletadoEn = ahora;
        }
        if (vencidos.Count > 0) await _pagos.UpdateAsync(vencidos[0]);

        if (enCurso.Count > vencidos.Count)
            throw ReglaNegocioException.Conflicto("pago_en_proceso",
                "Ya hay un pago en proceso para esta compra. Espera unos minutos antes de intentar de nuevo.");

        var pago = new Pago
        {
            CodigoTransaccion = await NuevoCodigoTransaccion(),
            CompraId = compra.Id,
            MedioPago = solicitud.MedioPago,
            Banco = solicitud.MedioPago == MedioPago.Pse ? solicitud.Banco : null,
            TipoCuenta = solicitud.MedioPago == MedioPago.Pse ? solicitud.TipoCuenta : null,
            CorreoComprobante = solicitud.CorreoComprobante.Trim(),
            Valor = compra.ValorTotal, // el valor siempre es el enviado por SSO, nunca el del cliente
            Estado = EstadoPago.Iniciado,
            CreadoEn = ahora
        };

        try
        {
            // El índice único parcial de la BD garantiza un solo pago "Iniciado" por compra.
            await _pagos.AddAsync(pago);
        }
        catch (DbUpdateException)
        {
            throw ReglaNegocioException.Conflicto("pago_en_proceso",
                "Ya hay un pago en proceso para esta compra. Espera unos minutos antes de intentar de nuevo.");
        }

        // 2) Cobrar en la pasarela.
        ResultadoCobro resultado;
        try
        {
            resultado = await _pasarela.Cobrar(new SolicitudCobro(
                pago.CodigoTransaccion, pago.Valor, pago.MedioPago, pago.Banco, pago.TipoCuenta, cedula, pago.CorreoComprobante));
        }
        catch (Exception ex)
        {
            // Resultado desconocido: NO se marca como rechazado (podría haberse cobrado). Queda "Iniciado" hasta vencer.
            _logger.LogError(ex, "Pasarela sin respuesta para la transacción {Codigo}", pago.CodigoTransaccion);
            throw ReglaNegocioException.PasarelaNoDisponible("pasarela_no_disponible",
                "No pudimos confirmar el estado de tu pago. No lo repitas todavía: revisa tu historial en unos minutos.");
        }

        // 3) Guardar el resultado.
        var completadoEn = _reloj.GetUtcNow();
        if (!resultado.Aprobado)
        {
            var codigo = resultado.CodigoFallo ?? "00";
            pago.Estado = EstadoPago.RechazadoBanco;
            pago.CodigoFallo = codigo;
            pago.MensajeFallo = resultado.MensajeFallo ?? "Pago rechazado";
            pago.CompletadoEn = completadoEn;
            await _pagos.UpdateAsync(pago);

            await EnviarResultadoRechazado(compra, pago);

            return new RespuestaPago(ResultadoPago.Rechazado, null,
                $"Pago rechazado por el banco (código {codigo}). No se notificó a SSO. Intenta con otro medio.");
        }

        pago.Estado = EstadoPago.Aprobado;
        pago.ReferenciaPasarela = resultado.Referencia ?? pago.CodigoTransaccion;
        pago.Instrumento = resultado.Instrumento ?? "Pago YoPago";
        pago.CompletadoEn = completadoEn;
        compra.Estado = EstadoCompra.Pagada;
        compra.PagadaEn = completadoEn;

        // Outbox: el aviso (webhook) a SSO se guarda en la MISMA transacción que el pago; si SSO o la red caen, se reintenta.
        var mensaje = new MensajeSalida
        {
            Tipo = TiposMensaje.PagoConfirmado,
            AgregadoId = pago.Id,
            Contenido = JsonSso.Serializar(new PagoConfirmadoWebhook(
                pago.CodigoTransaccion, compra.NumeroCompra, cedula, pago.Valor, completadoEn)),
            CreadoEn = completadoEn,
            ProximoIntentoEn = completadoEn
        };
        await _pagos.ConfirmarAsync(pago, compra, mensaje);

        await EnviarResultadoAprobado(cedula, pago.CodigoTransaccion);

        return new RespuestaPago(ResultadoPago.Aprobado, pago.CodigoTransaccion,
            "SSO será notificado y tu compra cambiará a estado «Pagada».");
    }

    public async Task<ComprobanteDto> ObtenerComprobante(string cedula, string codigoTransaccion)
    {
        var pago = await _pagos.GetComprobanteAsync(codigoTransaccion, cedula)
            ?? throw ReglaNegocioException.NoEncontrado("comprobante_no_encontrado", "No encontramos ese comprobante.");

        var notificadoEn = await _mensajes.GetProcesadoEnAsync(pago.Id, TiposMensaje.PagoConfirmado);

        return new ComprobanteDto(
            pago.CodigoTransaccion, pago.Compra.NumeroCompra, pago.Compra.Descripcion, pago.Compra.Cliente.Cedula,
            pago.Compra.Cliente.Nombre, pago.Instrumento ?? "—", pago.Valor, pago.Estado,
            pago.CompletadoEn ?? pago.CreadoEn, notificadoEn, pago.CodigoAcuseSso, pago.MensajeFallo, pago.CorreoComprobante);
    }

    public async Task<byte[]> ObtenerComprobantePdf(string cedula, string codigoTransaccion) =>
        _pdf.Generar(await ObtenerComprobante(cedula, codigoTransaccion));

    public async Task<EnvioComprobanteRespuesta> EnviarComprobantePorCorreo(string cedula, string codigoTransaccion)
    {
        var comprobante = await ObtenerComprobante(cedula, codigoTransaccion);
        var pdf = _pdf.Generar(comprobante);

        await _correo.Enviar(
            comprobante.CorreoComprobante,
            $"Comprobante YoPago {comprobante.CodigoTransaccion}",
            $"<p>Hola {System.Net.WebUtility.HtmlEncode(comprobante.NombreCliente)}, " +
            $"adjuntamos tu comprobante de pago de la compra {comprobante.NumeroCompra}.</p>",
            pdf,
            $"{comprobante.CodigoTransaccion}.pdf");

        return new EnvioComprobanteRespuesta(comprobante.CorreoComprobante);
    }

    // ---------------- correo con el resultado del pago (Gmail SMTP vía CorreoService) ----------------

    /// <summary>Si el correo falla, el pago ya quedó registrado: solo se deja el aviso en el log.</summary>
    private async Task EnviarResultadoAprobado(string cedula, string codigoTransaccion)
    {
        try
        {
            var c = await ObtenerComprobante(cedula, codigoTransaccion);
            await _correo.Enviar(
                c.CorreoComprobante,
                $"Pago aprobado · YoPago {c.CodigoTransaccion}",
                $"<p>Hola {WebUtility.HtmlEncode(c.NombreCliente)},</p>" +
                $"<p>Tu pago fue <strong>aprobado</strong>.</p>" +
                $"<ul><li>Compra: {WebUtility.HtmlEncode(c.NumeroCompra)} — {WebUtility.HtmlEncode(c.Descripcion)}</li>" +
                $"<li>Valor: {Moneda(c.Valor)}</li>" +
                $"<li>Medio: {WebUtility.HtmlEncode(c.Instrumento)}</li>" +
                $"<li>Transacción: {c.CodigoTransaccion}</li></ul>" +
                "<p>Adjuntamos tu comprobante. SSO ya fue notificado.</p>",
                _pdf.Generar(c),
                $"{c.CodigoTransaccion}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo enviar el correo de pago aprobado {Codigo}: {Error}", codigoTransaccion, ex.Message);
        }
    }

    private async Task EnviarResultadoRechazado(Compra compra, Pago pago)
    {
        try
        {
            await _correo.Enviar(
                pago.CorreoComprobante,
                $"Pago rechazado · YoPago {pago.CodigoTransaccion}",
                "<p>Tu pago fue <strong>rechazado</strong> y no se realizó ningún cobro.</p>" +
                $"<ul><li>Compra: {WebUtility.HtmlEncode(compra.NumeroCompra)} — {WebUtility.HtmlEncode(compra.Descripcion)}</li>" +
                $"<li>Valor: {Moneda(pago.Valor)}</li>" +
                $"<li>Motivo: {WebUtility.HtmlEncode(pago.MensajeFallo ?? "Pago rechazado")} (código {pago.CodigoFallo})</li>" +
                $"<li>Transacción: {pago.CodigoTransaccion}</li></ul>" +
                $"<p>Puedes intentarlo de nuevo con otro medio de pago en <a href=\"{_app.UrlFront.TrimEnd('/')}/compras\">YoPago</a>.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo enviar el correo de pago rechazado {Codigo}: {Error}", pago.CodigoTransaccion, ex.Message);
        }
    }

    private static string Moneda(decimal valor) => string.Format(new CultureInfo("es-CO"), "{0:C0}", valor);

    // ---------------- ayudas ----------------

    private static void ValidarPagable(EstadoCompra estado)
    {
        if (estado == EstadoCompra.Pagada)
            throw ReglaNegocioException.Conflicto("ya_pagada", "Esta compra ya fue pagada.");
        if (estado == EstadoCompra.Rechazada)
            throw ReglaNegocioException.Conflicto("compra_rechazada",
                "SSO rechazó la notificación de esta compra. Comunícate con soporte.");
    }

    private static void ValidarMedio(SolicitudPago solicitud)
    {
        if (!Enum.IsDefined(solicitud.MedioPago))
            throw ReglaNegocioException.Validacion("medio_invalido", "Medio de pago no válido.");
        if (solicitud.MedioPago != MedioPago.Pse) return;

        if (string.IsNullOrWhiteSpace(solicitud.Banco) || !CatalogoPagos.Bancos.Contains(solicitud.Banco))
            throw ReglaNegocioException.Validacion("banco_invalido", "Selecciona un banco válido.");
        if (string.IsNullOrWhiteSpace(solicitud.TipoCuenta) || !CatalogoPagos.TiposCuenta.Contains(solicitud.TipoCuenta))
            throw ReglaNegocioException.Validacion("tipo_cuenta_invalido", "Selecciona un tipo de cuenta válido.");
    }

    private async Task<string> NuevoCodigoTransaccion()
    {
        for (var i = 0; i < 5; i++)
        {
            var codigo = $"YP-{RandomNumberGenerator.GetInt32(10_000_000, 100_000_000)}";
            if (!await _pagos.ExisteCodigoAsync(codigo)) return codigo;
        }
        throw new InvalidOperationException("No fue posible generar un número de transacción único.");
    }
}

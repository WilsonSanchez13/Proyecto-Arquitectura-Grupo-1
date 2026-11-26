using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Backend.Configuracion;

namespace Backend.Services;

public class CorreoService
{
    private readonly SmtpOpciones _smtp;

    public CorreoService(IOptions<SmtpOpciones> smtp)
    {
        _smtp = smtp.Value;
    }

    public async Task Enviar(string destinatario, string asunto, string html,
        byte[]? adjunto = null, string? nombreAdjunto = null)
    {
        using var cliente = new SmtpClient(_smtp.Host, _smtp.Puerto) { EnableSsl = _smtp.UsarSsl, Timeout = 15_000 };
        if (!string.IsNullOrEmpty(_smtp.Usuario))
            cliente.Credentials = new NetworkCredential(_smtp.Usuario, _smtp.Clave);

        var remitente = string.IsNullOrWhiteSpace(_smtp.Remitente) ? _smtp.Usuario! : _smtp.Remitente; // Gmail solo deja enviar como la cuenta autenticada
        using var mensaje = new MailMessage(remitente, destinatario, asunto, html) { IsBodyHtml = true };
        if (adjunto is not null)
            mensaje.Attachments.Add(new Attachment(new MemoryStream(adjunto), nombreAdjunto ?? "adjunto.pdf", "application/pdf"));

        await cliente.SendMailAsync(mensaje);
    }
}

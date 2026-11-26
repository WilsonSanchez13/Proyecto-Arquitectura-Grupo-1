using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Backend.Dtos;

namespace Backend.Services;

public class ComprobantePdfService
{
    private static readonly NumberFormatInfo Pesos = new() { NumberGroupSeparator = ".", NumberDecimalDigits = 0 };
    private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5); // Colombia no usa horario de verano

    static ComprobantePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generar(ComprobanteDto c)
    {
        var valor = "$ " + c.Valor.ToString("N0", Pesos);
        var fecha = c.PagadoEn.ToOffset(Bogota).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        var sso = c.CodigoAcuseSso is not null ? $"Aceptada por SSO · {c.CodigoAcuseSso}"
            : c.NotificadoSsoEn is not null ? "Notificada a SSO, pendiente de confirmación"
            : "En cola para notificar a SSO";

        return Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A5);
            pagina.Margin(30);
            pagina.DefaultTextStyle(t => t.FontSize(11));

            pagina.Header().Column(columna =>
            {
                columna.Item().Text("YoPago").FontSize(20).Bold().FontColor("#0b4a2f");
                columna.Item().Text("Comprobante de pago").FontSize(13);
            });

            pagina.Content().PaddingVertical(16).Column(columna =>
            {
                columna.Spacing(7);
                Linea(columna, "N.º transacción", c.CodigoTransaccion);
                Linea(columna, "N.º compra SSO", c.NumeroCompra);
                Linea(columna, "Bien", c.Descripcion);
                Linea(columna, "Cliente", c.NombreCliente);
                Linea(columna, "Cédula", c.Cedula);
                Linea(columna, "Medio de pago", c.Instrumento);
                Linea(columna, "Fecha", fecha);
                Linea(columna, "SSO", sso);
                columna.Item().PaddingTop(10).Row(fila =>
                {
                    fila.RelativeItem().Text("Valor pagado").Bold().FontSize(13);
                    fila.RelativeItem().AlignRight().Text(valor).Bold().FontSize(13);
                });
            });

            pagina.Footer().AlignCenter().Text("Presenta este comprobante y tu cédula en la sede SSO.").FontSize(9);
        })).GeneratePdf();
    }

    private static void Linea(ColumnDescriptor columna, string etiqueta, string valor)
    {
        columna.Item().Row(fila =>
        {
            fila.RelativeItem().Text(etiqueta).FontColor(Colors.Grey.Darken1);
            fila.RelativeItem(1.4f).AlignRight().Text(valor).SemiBold();
        });
    }
}

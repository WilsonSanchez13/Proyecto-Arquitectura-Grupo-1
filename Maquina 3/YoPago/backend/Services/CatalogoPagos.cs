using Backend.Dtos;
using Backend.Models;

namespace Backend.Services;

/// <summary>Catálogo de medios de pago, bancos y tipos de cuenta. Vive en el backend: el front solo lo pinta.</summary>
public static class CatalogoPagos
{
    public static readonly List<MedioPagoOpcion> Medios = new()
    {
        new(MedioPago.Pse, "PSE — Débito bancario", "Todos los bancos · Confirmación inmediata", true, true),
        new(MedioPago.Tarjeta, "Tarjeta crédito / débito", "Visa · Mastercard · Amex", false, false),
        new(MedioPago.Nequi, "Nequi / Bancolombia", "Aprobación con push en tu celular", false, false),
        new(MedioPago.Consignacion, "Consignación / Efecty", "Referencia válida por 48 horas", false, false)
    };

    public static readonly List<string> Bancos = new()
    {
        "Bancolombia", "Davivienda", "Banco de Bogotá", "BBVA", "Banco de Occidente", "Banco Popular",
        "Scotiabank Colpatria", "Itaú", "Banco Caja Social", "AV Villas"
    };

    public static readonly List<string> TiposCuenta = new() { "Ahorros", "Corriente" };
}

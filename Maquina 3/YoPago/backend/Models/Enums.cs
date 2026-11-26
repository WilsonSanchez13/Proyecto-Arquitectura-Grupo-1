namespace Backend.Models;

public enum EstadoCompra
{
    Pendiente,
    Pagada,
    /// <summary>SSO rechazó la notificación del pago.</summary>
    Rechazada
}

public enum EstadoPago
{
    /// <summary>Intento creado, esperando respuesta de la pasarela.</summary>
    Iniciado,
    Aprobado,
    RechazadoBanco,
    RechazadoSso,
    /// <summary>Intento que nunca recibió respuesta y venció.</summary>
    Vencido
}

public enum MedioPago
{
    Pse,
    Tarjeta,
    Nequi,
    Consignacion
}

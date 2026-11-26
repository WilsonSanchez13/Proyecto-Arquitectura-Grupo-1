using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public class LoginSolicitud
{
    [Required, StringLength(20, MinimumLength = 5)] public string Cedula { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 1)] public string Clave { get; set; } = string.Empty;
}

public record UsuarioSesion(string Cedula, string Nombre);

public record LoginRespuesta(string TokenAcceso, DateTimeOffset VenceEn, UsuarioSesion Usuario);

public class RecuperacionSolicitud
{
    [Required, StringLength(20, MinimumLength = 5)] public string Cedula { get; set; } = string.Empty;
}

public class RecuperacionConfirmacion
{
    [Required, StringLength(20, MinimumLength = 5)] public string Cedula { get; set; } = string.Empty;
    [Required, StringLength(200, MinimumLength = 20)] public string Token { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 10)] public string ClaveNueva { get; set; } = string.Empty;
}

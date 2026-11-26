using System.Security.Claims;
using Backend.Excepciones;

namespace Backend.Extensiones;

public static class UsuarioExtensiones
{
    /// <summary>Cédula del cliente autenticado, tomada del token (nunca de la URL ni del cuerpo).</summary>
    public static string ObtenerCedula(this ClaimsPrincipal usuario) =>
        usuario.FindFirst("cedula")?.Value
        ?? throw ReglaNegocioException.NoAutorizado("token_invalido", "La sesión no es válida.");
}

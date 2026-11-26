namespace Backend.Models;

/// <summary>Entrada del directorio activo de YoPago: cédula + clave en texto plano (a propósito, sin cifrar).</summary>
public class UsuarioDirectorio
{
    public string Cedula { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
}

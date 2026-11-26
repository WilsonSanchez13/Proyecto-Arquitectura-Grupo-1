namespace Backend.Excepciones;

/// <summary>Error de negocio con código estable y estado HTTP; el middleware lo convierte en ProblemDetails.</summary>
public class ReglaNegocioException : Exception
{
    public string Codigo { get; }
    public int EstadoHttp { get; }
    public Dictionary<string, object?> Extensiones { get; } = new();

    public ReglaNegocioException(string codigo, string mensaje, int estadoHttp) : base(mensaje)
    {
        Codigo = codigo;
        EstadoHttp = estadoHttp;
    }

    public static ReglaNegocioException Validacion(string codigo, string mensaje) => new(codigo, mensaje, 400);
    public static ReglaNegocioException NoAutorizado(string codigo, string mensaje) => new(codigo, mensaje, 401);
    public static ReglaNegocioException NoEncontrado(string codigo, string mensaje) => new(codigo, mensaje, 404);
    public static ReglaNegocioException Conflicto(string codigo, string mensaje) => new(codigo, mensaje, 409);
    public static ReglaNegocioException Bloqueado(string codigo, string mensaje) => new(codigo, mensaje, 423);
    public static ReglaNegocioException PasarelaNoDisponible(string codigo, string mensaje) => new(codigo, mensaje, 502);
}

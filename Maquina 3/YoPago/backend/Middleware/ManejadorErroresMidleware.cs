using Microsoft.AspNetCore.Mvc;
using Backend.Excepciones;

namespace Backend.Middleware;

/// <summary>Convierte cualquier error en un ProblemDetails con un "codigo" estable que el front puede mostrar o ignorar.</summary>
public class ManejadorErroresMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorErroresMiddleware> _logger;

    public ManejadorErroresMiddleware(RequestDelegate siguiente, ILogger<ManejadorErroresMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _siguiente(context);
        }
        catch (ReglaNegocioException ex)
        {
            var problema = new ProblemDetails { Status = ex.EstadoHttp, Title = ex.Codigo, Detail = ex.Message };
            problema.Extensions["codigo"] = ex.Codigo;
            foreach (var (clave, valor) in ex.Extensiones) problema.Extensions[clave] = valor;
            await Escribir(context, problema);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499; // el cliente cerró la conexión
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
            var problema = new ProblemDetails
            {
                Status = 500,
                Title = "error_inesperado",
                Detail = "Ocurrió un error inesperado. Intenta de nuevo en unos minutos."
            };
            problema.Extensions["codigo"] = "error_inesperado";
            await Escribir(context, problema);
        }
    }

    private static Task Escribir(HttpContext context, ProblemDetails problema)
    {
        context.Response.StatusCode = problema.Status ?? 500;
        return context.Response.WriteAsJsonAsync(problema, options: null, contentType: "application/problem+json");
    }
}
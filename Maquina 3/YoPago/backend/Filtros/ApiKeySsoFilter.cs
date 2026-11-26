using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Backend.Configuracion;
using Backend.Excepciones;

namespace Backend.Filtros;

/// <summary>Exige que las llamadas de SSO traigan el encabezado X-Api-Key con la clave acordada.</summary>
public class ApiKeySsoFilter : IAsyncActionFilter
{
    private readonly SsoOpciones _sso;

    public ApiKeySsoFilter(IOptions<SsoOpciones> sso)
    {
        _sso = sso.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var recibida);

        var valida = !string.IsNullOrEmpty(_sso.ApiKeyEntrante)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(recibida.ToString()),
                Encoding.UTF8.GetBytes(_sso.ApiKeyEntrante));

        if (!valida)
            throw ReglaNegocioException.NoAutorizado("api_key_invalida", "Falta la clave X-Api-Key o no es válida.");

        await next();
    }
}
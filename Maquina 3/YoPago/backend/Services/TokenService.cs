using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Backend.Configuracion;
using Backend.Models;

namespace Backend.Services;

public class TokenService
{
    private readonly JwtOpciones _jwt;
    private readonly TimeProvider _reloj;

    public TokenService(IOptions<JwtOpciones> jwt, TimeProvider reloj)
    {
        _jwt = jwt.Value;
        _reloj = reloj;
    }

    public (string Token, DateTimeOffset Vence) Crear(Cliente cliente)
    {
        var ahora = _reloj.GetUtcNow();
        var vence = ahora.AddMinutes(_jwt.MinutosVigencia);
        var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Clave));

        var token = new JwtSecurityToken(
            _jwt.Emisor,
            _jwt.Audiencia,
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, cliente.Id.ToString()),
                new Claim("cedula", cliente.Cedula),
                new Claim("nombre", cliente.Nombre)
            },
            notBefore: ahora.UtcDateTime,
            expires: vence.UtcDateTime,
            signingCredentials: new SigningCredentials(llave, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), vence);
    }
}

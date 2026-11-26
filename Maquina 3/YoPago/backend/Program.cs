using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Backend.Configuracion;
using Backend.Data;
using Backend.Filtros;
using Backend.Middleware;
using Backend.Repositories;
using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar Kestrel
var puerto = builder.Configuration.GetValue("Puerto", 8000);
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(puerto); // HTTP
});

// Opciones (appsettings.json)
builder.Services.Configure<AppOpciones>(builder.Configuration.GetSection("App"));
builder.Services.Configure<SeguridadOpciones>(builder.Configuration.GetSection("Seguridad"));
builder.Services.Configure<SsoOpciones>(builder.Configuration.GetSection("Sso"));
builder.Services.Configure<JwtOpciones>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SmtpOpciones>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<PasarelaOpciones>(builder.Configuration.GetSection("Pasarela"));

// Agregar servicios
builder.Services.AddDbContext<YoPagoContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("YoPago"))
       .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<ICompraRepository, CompraRepository>();
builder.Services.AddScoped<IPagoRepository, PagoRepository>();
builder.Services.AddScoped<IMensajeSalidaRepository, MensajeSalidaRepository>();

builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<CorreoService>();
builder.Services.AddSingleton<ComprobantePdfService>();
builder.Services.AddSingleton<PasarelaPagoService>();

builder.Services.Configure<DirectorioOpciones>(builder.Configuration.GetSection("Directorio"));
builder.Services.Configure<LdapOpciones>(builder.Configuration.GetSection("Ldap"));
if (string.Equals(builder.Configuration["Directorio:Modo"], "Ldap", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IDirectorioActivo, DirectorioLdapService>();
else
    builder.Services.AddScoped<IDirectorioActivo, DirectorioLocalService>();
builder.Services.AddScoped<AutenticacionService>();
builder.Services.AddScoped<CompraService>();
builder.Services.AddScoped<PagoService>();
builder.Services.AddScoped<ComprasSsoService>();
builder.Services.AddScoped<AcuseSsoService>();
builder.Services.AddScoped<ApiKeySsoFilter>();

// Integración REST con SSO: cliente HTTP hacia el webhook de SSO + envío en segundo plano con reintentos
builder.Services.AddHttpClient<ProxySsoService>();
builder.Services.AddHostedService<PublicadorMensajesService>();

// Autenticación JWT
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOpciones>() ?? new JwtOpciones();
if (jwt.Clave.Length < 32)
    throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 caracteres (usa la variable Jwt__Clave).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
{
    opt.MapInboundClaims = false;
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Emisor,
        ValidAudience = jwt.Audiencia,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(opt => opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(opt => opt.InvalidModelStateResponseFactory = contexto =>
    {
        var problema = new ValidationProblemDetails(contexto.ModelState)
        {
            Status = 400, Title = "validacion_fallida", Detail = "Revisa los datos ingresados."
        };
        problema.Extensions["codigo"] = "validacion_fallida";
        return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
    });

// CORS (solo desarrollo; en producción el front y la API comparten origen vía nginx)
var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(politica =>
{
    if (origenes.Length > 0) politica.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod();
}));

// Límite de peticiones para login / recuperación
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.AddPolicy("auth", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.Configure<ForwardedHeadersOptions>(opt =>
{
    opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opt.KnownNetworks.Clear();
    opt.KnownProxies.Clear();
});

builder.Services.AddHealthChecks().AddDbContextCheck<YoPagoContext>("postgres");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "YoPago API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Token obtenido en POST /api/autenticacion/login"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }]
            = Array.Empty<string>()
    });
});

var app = builder.Build();

// Crea la base y las tablas si faltan y, en desarrollo, carga los datos de prueba.
if (app.Configuration.GetValue("BaseDatos:CrearAlIniciar", true))
{
    using var scope = app.Services.CreateScope();
    await InicializadorBaseDatos.Inicializar(scope.ServiceProvider, app.Configuration);
}

// Middleware
app.UseForwardedHeaders();
app.UseMiddleware<ManejadorErroresMiddleware>();

if (app.Configuration.GetValue("Swagger:Habilitado", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

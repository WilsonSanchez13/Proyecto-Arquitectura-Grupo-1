namespace Backend.Configuracion;

public class AppOpciones
{
    public string UrlFront { get; set; } = "http://localhost:4200";
    /// <summary>Días antes del vencimiento a partir de los cuales una compra se marca "por vencer".</summary>
    public int DiasPorVencer { get; set; } = 3;
    public int MinutosIntentoPago { get; set; } = 10;
}

public class SeguridadOpciones
{
    public int MaxIntentosFallidos { get; set; } = 3;
    public int MinutosBloqueo { get; set; } = 15;
    public int MinutosTokenRecuperacion { get; set; } = 30;
}

/// <summary>Integración REST con SSO (ver docs/contrato-sso.md).</summary>
public class SsoOpciones
{
    /// <summary>Clave que SSO debe enviar en el encabezado X-Api-Key al llamar a YoPago.</summary>
    public string ApiKeyEntrante { get; set; } = string.Empty;
    /// <summary>Clave que YoPago envía en X-Api-Key al llamar al webhook de SSO.</summary>
    public string ApiKeySaliente { get; set; } = string.Empty;
    /// <summary>URL del webhook de SSO que recibe los pagos confirmados (POST).</summary>
    public string UrlWebhookPagos { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 10;
}

public class JwtOpciones
{
    public string Emisor { get; set; } = "yopago-api";
    public string Audiencia { get; set; } = "yopago-web";
    /// <summary>Mínimo 32 caracteres. En producción viene de una variable de entorno / secreto.</summary>
    public string Clave { get; set; } = string.Empty;
    public int MinutosVigencia { get; set; } = 20;
}

public class SmtpOpciones
{
    public string Host { get; set; } = "localhost";
    public int Puerto { get; set; } = 1025;
    public string Remitente { get; set; } = "medicat227@gmail.com.co";
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
    public bool UsarSsl { get; set; }
}

public class DirectorioOpciones
{
    /// <summary>"Local" (tabla directorio_activo) o "Ldap" (AD / servidor LDAP real).</summary>
    public string Modo { get; set; } = "Local";
}

/// <summary>Conexión al Active Directory / LDAP cuando Directorio:Modo = "Ldap".</summary>
public class LdapOpciones
{
    public string Host { get; set; } = "localhost";
    /// <summary>389 = LDAP (con StartTLS si UsarStartTls), 636 = LDAPS.</summary>
    public int Puerto { get; set; } = 389;
    public bool UsarSsl { get; set; }
    public bool UsarStartTls { get; set; }
    /// <summary>Cómo se arma el usuario para el bind. AD: "{cedula}@yopago.local" (UPN). OpenLDAP: "uid={cedula},ou=clientes,dc=yopago,dc=local".</summary>
    public string PlantillaUsuario { get; set; } = "uid={cedula},ou=clientes,dc=yopago,dc=local";
    /// <summary>Dónde se buscan/crean usuarios al guardar una clave (cuenta de servicio).</summary>
    public string BaseUsuarios { get; set; } = "ou=clientes,dc=yopago,dc=local";
    /// <summary>Atributo que contiene la cédula: "sAMAccountName" en AD, "uid" en OpenLDAP.</summary>
    public string AtributoLogin { get; set; } = "uid";
    /// <summary>"userPassword" (OpenLDAP) o "unicodePwd" (AD; exige LDAPS).</summary>
    public string AtributoClave { get; set; } = "userPassword";
    /// <summary>Cuenta de servicio con permiso de crear usuarios y cambiar claves.</summary>
    public string UsuarioAdmin { get; set; } = string.Empty;
    public string ClaveAdmin { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 10;
}

public class PasarelaOpciones
{
    public int LatenciaMs { get; set; } = 600;
    /// <summary>Cédulas cuyo pago se rechaza con código 51 (fondos insuficientes). Útil para probar el flujo de error.</summary>
    public List<string> CedulasRechazadas { get; set; } = new();
}

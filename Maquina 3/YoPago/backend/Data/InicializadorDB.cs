using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Backend.Data;

/// <summary>Al arrancar: crea la base si no existe, ejecuta schema.sql (tablas) y, en desarrollo, data.sql (datos de prueba).</summary>
public static class InicializadorBaseDatos
{
    public static async Task Inicializar(IServiceProvider servicios, IConfiguration config)
    {
        var logger = servicios.GetRequiredService<ILoggerFactory>().CreateLogger("InicializadorBaseDatos");
        var cadena = config.GetConnectionString("YoPago")!;

        try
        {
            await CrearBaseSiNoExiste(cadena, logger);

            var context = servicios.GetRequiredService<YoPagoContext>();
            await context.Database.ExecuteSqlRawAsync(LeerScript("schema.sql"));

            if (config.GetValue("BaseDatos:CargarDatosPrueba", false))
            {
                await context.Database.ExecuteSqlRawAsync(LeerScript("data.sql"));
                logger.LogInformation("Datos de prueba cargados (data.sql): cédula {Cedula}, clave {Clave}",
                    DatosPrueba.Cedula, DatosPrueba.Clave);
            }
        }
        catch (NpgsqlException ex)
        {
            var b = new NpgsqlConnectionStringBuilder(cadena);
            logger.LogError("No se pudo usar PostgreSQL en {Host}:{Puerto} con el usuario '{Usuario}': {Error}. " +
                            "Revisa ConnectionStrings:YoPago en appsettings.Development.json.", b.Host, b.Port, b.Username, ex.Message);
            throw;
        }
    }

    private static async Task CrearBaseSiNoExiste(string cadena, ILogger logger)
    {
        var builder = new NpgsqlConnectionStringBuilder(cadena);
        var nombre = builder.Database!;
        builder.Database = "postgres"; // base de mantenimiento

        await using var conexion = new NpgsqlConnection(builder.ConnectionString);
        await conexion.OpenAsync();

        await using var consulta = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @nombre", conexion);
        consulta.Parameters.AddWithValue("nombre", nombre);
        if (await consulta.ExecuteScalarAsync() is not null) return;

        await using var crear = new NpgsqlCommand($"CREATE DATABASE \"{nombre.Replace("\"", "\"\"")}\"", conexion);
        await crear.ExecuteNonQueryAsync();
        logger.LogInformation("Base de datos {Base} creada", nombre);
    }

    private static string LeerScript(string nombre)
    {
        using var flujo = Assembly.GetExecutingAssembly().GetManifestResourceStream(nombre)
            ?? throw new InvalidOperationException($"No se encontró {nombre} embebido.");
        using var lector = new StreamReader(flujo);
        return lector.ReadToEnd();
    }
}

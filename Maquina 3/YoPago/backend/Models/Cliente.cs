namespace Backend.Models;

public class Cliente
{
    public int Id { get; set; }
    public string Cedula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int IntentosFallidos { get; set; }
    public DateTimeOffset? BloqueadoHasta { get; set; }
    public string? TokenRecuperacionHash { get; set; }
    public DateTimeOffset? TokenRecuperacionVence { get; set; }
    public DateTimeOffset CreadoEn { get; set; }

    public List<Compra> Compras { get; set; } = new();
}

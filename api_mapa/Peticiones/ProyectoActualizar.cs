namespace ApiMapa.Peticiones;

/// <summary>
/// El cuerpo del PATCH: TODOS los campos son opcionales, y solo se escriben los
/// que lleguen.
///
/// La diferencia con ProyectoReemplazo es la lección del contrato: el MISMO
/// cuerpo responde 422 en PUT y 200 en PATCH, y no lo decide un if en el
/// servicio — lo decide el tipo.
/// </summary>
public class ProyectoActualizar
{
    public string? Titulo { get; set; }
    public string? Resumen { get; set; }
    public double? Presupuesto { get; set; }
    public string? TipoFinanciacion { get; set; }
    public string? TipoFondos { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace ApiMapa.Peticiones;

/// <summary>
/// El cuerpo del PUT. Reemplazar es poner TODO de nuevo, así que los seis
/// obligatorios lo siguen siendo. El id no va aquí: identifica la fila y viaja
/// en la ruta.
/// </summary>
public class ProyectoReemplazo
{
    [Required(ErrorMessage = "El campo titulo es obligatorio.")]
    [MaxLength(70, ErrorMessage = "El campo titulo no puede exceder los 70 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo resumen es obligatorio.")]
    [MaxLength(256, ErrorMessage = "El campo resumen no puede exceder los 256 caracteres.")]
    public string Resumen { get; set; } = string.Empty;

    /// <summary>Un número, no una cadena: si llega "mucho", el framework responde
    /// 422 antes de que el negocio se entere (C8).</summary>
    [Required(ErrorMessage = "El campo presupuesto es obligatorio.")]
    public double? Presupuesto { get; set; }

    [Required(ErrorMessage = "El campo tipoFinanciacion es obligatorio.")]
    [MaxLength(45, ErrorMessage = "El campo tipoFinanciacion no puede exceder los 45 caracteres.")]
    public string TipoFinanciacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo tipoFondos es obligatorio.")]
    [MaxLength(45, ErrorMessage = "El campo tipoFondos no puede exceder los 45 caracteres.")]
    public string TipoFondos { get; set; } = string.Empty;

    /// <summary>Una fecha, no una cadena: si llega "ayer", es 422.</summary>
    [Required(ErrorMessage = "El campo fechaInicio es obligatorio.")]
    public DateTime? FechaInicio { get; set; }

    /// <summary>El único opcional: un proyecto en curso no tiene fecha de fin.</summary>
    public DateTime? FechaFin { get; set; }
}

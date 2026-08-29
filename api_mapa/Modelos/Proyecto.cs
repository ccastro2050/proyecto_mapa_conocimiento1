namespace ApiMapa.Modelos;

/// <summary>
/// Entidad de dominio que representa la tabla proyecto: un proyecto de
/// investigación. Es lo que viaja entre las capas.
///
/// No incluye Activo: el borrado lógico es un detalle interno del motor y no
/// forma parte de lo que la API expone (5_data_model.md §4).
///
/// Es la tabla más rica en TIPOS de las cuatro del curso: aquí las fechas son
/// DateTime de verdad —porque el esquema las declara DATE, no VARCHAR— y el
/// presupuesto es un double. Eso hace que el tipo también sea una regla: un
/// presupuesto que llegue como "mucho" es 422 y no llega a la base (C8).
/// </summary>
public class Proyecto
{
    /// <summary>El código del proyecto. Es la llave primaria.</summary>
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    /// <summary>El campo más largo de la tabla: 256 caracteres.</summary>
    public string Resumen { get; set; } = string.Empty;

    /// <summary>Un número con decimales, NO una cadena (C8).</summary>
    public double Presupuesto { get; set; }

    /// <summary>Interna, externa, mixta…</summary>
    public string TipoFinanciacion { get; set; } = string.Empty;

    public string TipoFondos { get; set; } = string.Empty;

    /// <summary>Fecha de verdad: la columna es DATE, no texto.</summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>El ÚNICO campo que admite nulos: un proyecto en curso todavía no
    /// tiene fecha de fin (C7).</summary>
    public DateTime? FechaFin { get; set; }
}

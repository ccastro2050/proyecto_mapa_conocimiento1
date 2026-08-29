using ApiMapa.Modelos;

namespace ApiMapa.Repositorios;

/// <summary>
/// El contrato de la capa de datos. El servicio conoce ESTA interfaz y nada más:
/// no sabe que detrás hay SQL Server, y por eso se le puede enchufar un
/// repositorio de mentiras para probarlo sin base de datos (Artículo 3).
/// </summary>
public interface IRepositorioProyecto
{
    Task<IEnumerable<Proyecto>> ObtenerTodos(int limite);
    Task<Proyecto?> ObtenerPorId(int id);
    Task Crear(Proyecto proyecto);
    Task<int> Reemplazar(Proyecto proyecto);
    Task<int> ActualizarParcial(int id, ProyectoCampos campos);
    Task<int> EliminarLogico(int id);
}

/// <summary>
/// Los campos que un PATCH puede traer, todos opcionales.
///
/// Este tipo los agrupa SIN que la capa 2 ni la 3 conozcan las clases de
/// Peticiones/, que son la frontera HTTP (3_plan.md §4.7).
/// </summary>
public record ProyectoCampos(
    string? Titulo = null,
    string? Resumen = null,
    double? Presupuesto = null,
    string? TipoFinanciacion = null,
    string? TipoFondos = null,
    DateTime? FechaInicio = null,
    DateTime? FechaFin = null)
{
    /// <summary>¿Llegó algún campo? Si no, el PATCH es un 400 y no un 404.</summary>
    public bool HayAlguno =>
        Titulo != null || Resumen != null || Presupuesto != null
        || TipoFinanciacion != null || TipoFondos != null
        || FechaInicio != null || FechaFin != null;
}

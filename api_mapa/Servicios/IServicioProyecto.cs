using ApiMapa.Modelos;
using ApiMapa.Repositorios;

namespace ApiMapa.Servicios;

/// <summary>
/// El contrato de la capa de negocio. Solo conoce Modelos/ y el tipo de campos
/// parciales: las clases de Peticiones/ pertenecen a la frontera HTTP y no cruzan
/// a esta capa (3_plan.md §4.7).
///
/// Los problemas se comunican con excepciones, que el controlador traduce:
///   ArgumentException      → 400
///   NoEncontradoExcepcion  → 404
/// </summary>
public interface IServicioProyecto
{
    /// <summary>Hasta 'limite' proyectos activos. ArgumentException si limite &lt;= 0.</summary>
    Task<IEnumerable<Proyecto>> ObtenerTodos(int limite);

    /// <summary>El proyecto con ese código. NoEncontradoExcepcion si no existe o está inactivo.</summary>
    Task<Proyecto> ObtenerPorId(int id);

    /// <summary>Crea el proyecto. El cuerpo ya fue validado por ProyectoCrear.</summary>
    Task Crear(Proyecto proyecto);

    /// <summary>Reemplazo completo. NoEncontradoExcepcion si no existe · devuelve filas afectadas.</summary>
    Task<int> Reemplazar(int id, Proyecto proyecto);

    /// <summary>Escribe solo los campos enviados. ArgumentException si no llegó ninguno ·
    /// NoEncontradoExcepcion si no existe · devuelve filas afectadas.</summary>
    Task<int> ActualizarParcial(int id, ProyectoCampos campos);

    /// <summary>Borrado lógico. NoEncontradoExcepcion si no existe o ya estaba inactivo.</summary>
    Task<int> Eliminar(int id);
}

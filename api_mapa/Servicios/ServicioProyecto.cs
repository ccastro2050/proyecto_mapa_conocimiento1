using ApiMapa.Excepciones;
using ApiMapa.Modelos;
using ApiMapa.Repositorios;

namespace ApiMapa.Servicios;

/// <summary>
/// La capa 2: las reglas de negocio. Depende solo de la interfaz del repositorio,
/// así que no sabe qué motor hay detrás (Artículo 3) — y por eso se puede probar
/// sin base de datos.
/// </summary>
public class ServicioProyecto : IServicioProyecto
{
    private readonly IRepositorioProyecto _repositorio;

    public ServicioProyecto(IRepositorioProyecto repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<IEnumerable<Proyecto>> ObtenerTodos(int limite)
    {
        // La FORMA del dato es correcta (sí es un entero), así que esto es 400
        // y no 422 (C10).
        if (limite <= 0)
        {
            throw new ArgumentException("El parámetro limite debe ser un número mayor a 0.");
        }

        return await _repositorio.ObtenerTodos(limite);
    }

    public async Task<Proyecto> ObtenerPorId(int id)
    {
        var proyecto = await _repositorio.ObtenerPorId(id);
        if (proyecto == null)
        {
            throw new NoEncontradoExcepcion($"No existe un proyecto con el código {id}.");
        }

        return proyecto;
    }

    public async Task Crear(Proyecto proyecto)
    {
        await _repositorio.Crear(proyecto);
    }

    public async Task<int> Reemplazar(int id, Proyecto proyecto)
    {
        // El código identifica la fila y viene de la ruta, no del cuerpo
        proyecto.Id = id;

        var filasAfectadas = await _repositorio.Reemplazar(proyecto);
        if (filasAfectadas == 0)
        {
            throw new NoEncontradoExcepcion($"No existe un proyecto con el código {id}.");
        }

        return filasAfectadas;
    }

    public async Task<int> ActualizarParcial(int id, ProyectoCampos campos)
    {
        // Sin esta comprobación el repositorio devolvería 0 filas, que en toda la
        // demás lógica significa "no existe" — y responderíamos 404 en vez del 400
        // que exige el contrato para un cuerpo vacío.
        if (!campos.HayAlguno)
        {
            throw new ArgumentException("No se envió ningún campo para actualizar.");
        }

        var filasAfectadas = await _repositorio.ActualizarParcial(id, campos);
        if (filasAfectadas == 0)
        {
            throw new NoEncontradoExcepcion($"No existe un proyecto con el código {id}.");
        }

        return filasAfectadas;
    }

    public async Task<int> Eliminar(int id)
    {
        var filasAfectadas = await _repositorio.EliminarLogico(id);
        if (filasAfectadas == 0)
        {
            throw new NoEncontradoExcepcion($"No existe un proyecto con el código {id}.");
        }

        return filasAfectadas;
    }
}

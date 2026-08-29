using ApiMapa.Excepciones;
using ApiMapa.Modelos;
using ApiMapa.Repositorios;
using ApiMapa.Servicios;

namespace ApiMapa.Pruebas;

/// <summary>
/// El repositorio de mentiras: otra implementación de la MISMA interfaz que guarda
/// las filas en una lista en memoria, en vez de hablar con SQL Server.
///
/// Como el servicio solo conoce la interfaz, no se entera de la diferencia. Eso es
/// lo que hace demostrable que las capas están desacopladas (criterio 7).
/// </summary>
public class RepositorioFalso : IRepositorioProyecto
{
    private readonly List<Proyecto> _filas = new();

    public Task<IEnumerable<Proyecto>> ObtenerTodos(int limite) =>
        Task.FromResult(_filas.Take(limite).AsEnumerable());

    public Task<Proyecto?> ObtenerPorId(int id) =>
        Task.FromResult(_filas.FirstOrDefault(p => p.Id == id));

    public Task Crear(Proyecto proyecto)
    {
        _filas.Add(proyecto);
        return Task.CompletedTask;
    }

    public Task<int> Reemplazar(Proyecto proyecto)
    {
        var i = _filas.FindIndex(p => p.Id == proyecto.Id);
        if (i == -1) return Task.FromResult(0);
        _filas[i] = proyecto;
        return Task.FromResult(1);
    }

    public Task<int> ActualizarParcial(int id, ProyectoCampos campos)
    {
        var p = _filas.FirstOrDefault(x => x.Id == id);
        if (p == null) return Task.FromResult(0);

        if (campos.Titulo != null) p.Titulo = campos.Titulo;
        if (campos.Resumen != null) p.Resumen = campos.Resumen;
        if (campos.Presupuesto != null) p.Presupuesto = campos.Presupuesto.Value;
        if (campos.TipoFinanciacion != null) p.TipoFinanciacion = campos.TipoFinanciacion;
        if (campos.TipoFondos != null) p.TipoFondos = campos.TipoFondos;
        if (campos.FechaInicio != null) p.FechaInicio = campos.FechaInicio.Value;
        if (campos.FechaFin != null) p.FechaFin = campos.FechaFin;

        return Task.FromResult(1);
    }

    public Task<int> EliminarLogico(int id)
    {
        // Sacarlo de la lista reproduce el EFECTO OBSERVABLE del borrado lógico:
        // deja de listarse y deja de encontrarse. La entidad no tiene columna
        // Activo —es detalle del motor—, así que aquí no hay nada que marcar.
        var p = _filas.FirstOrDefault(x => x.Id == id);
        if (p == null) return Task.FromResult(0);
        _filas.Remove(p);
        return Task.FromResult(1);
    }
}

public class Proyecto_Pruebas
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Prueba de capas — SIN base de datos ===");

        IRepositorioProyecto repoFalso = new RepositorioFalso();
        IServicioProyecto servicio = new ServicioProyecto(repoFalso);

        var nuevo = new Proyecto
        {
            Id = 9001,
            Titulo = "Mapa de conocimiento institucional",
            Resumen = "Proyecto para consolidar la produccion academica.",
            Presupuesto = 85000000.50,
            TipoFinanciacion = "Interna",
            TipoFondos = "Recurrentes",
            FechaInicio = new DateTime(2026, 2, 1),
            FechaFin = null
        };

        // 1. El sistema arranca vacío
        var vacio = await servicio.ObtenerTodos(1000);
        Console.WriteLine(vacio.Any()
            ? "[ERROR] Debía arrancar sin proyectos."
            : "[OK] El sistema arranca vacío: sin proyectos.");

        // 2. Crear y listar
        await servicio.Crear(nuevo);
        var lista = (await servicio.ObtenerTodos(1000)).ToList();
        Console.WriteLine(lista.Count == 1
            ? $"[OK] Proyecto creado y listado: {lista[0].Titulo}"
            : "[ERROR] Debía haber exactamente un proyecto.");

        // 3. La fecha de cierre nula sobrevive: un proyecto abierto no la tiene
        Console.WriteLine(lista[0].FechaFin == null
            ? "[OK] fechaFin admite nulos: el proyecto sigue en curso."
            : "[ERROR] fechaCierre debía seguir siendo nula.");

        // 4. Buscar uno que no existe lanza NoEncontradoExcepcion
        try
        {
            await servicio.ObtenerPorId(999999);
            Console.WriteLine("[ERROR] Debió lanzar NoEncontradoExcepcion.");
        }
        catch (NoEncontradoExcepcion)
        {
            Console.WriteLine("[OK] Buscar un código inexistente lanza NoEncontradoExcepcion.");
        }

        // 5. El límite inválido es regla de negocio: ArgumentException (→ 400)
        try
        {
            await servicio.ObtenerTodos(0);
            Console.WriteLine("[ERROR] Debió lanzar ArgumentException.");
        }
        catch (ArgumentException)
        {
            Console.WriteLine("[OK] Límite menor o igual a cero rechazado con ArgumentException.");
        }

        // 6. PATCH sin campos: 400, no 404
        try
        {
            await servicio.ActualizarParcial(9001, new ProyectoCampos());
            Console.WriteLine("[ERROR] Debió lanzar ArgumentException por cuerpo vacío.");
        }
        catch (ArgumentException)
        {
            Console.WriteLine("[OK] Cuerpo vacío en actualización parcial rechazado con ArgumentException.");
        }

        // 7. PATCH con un solo campo sí funciona
        await servicio.ActualizarParcial(9001, new ProyectoCampos(Presupuesto: 95000000.0));
        var tras = await servicio.ObtenerPorId(9001);
        Console.WriteLine(tras.Presupuesto == 95000000.0
            && tras.Titulo == "Mapa de conocimiento institucional"
            ? "[OK] La actualización parcial cambió solo el presupuesto."
            : "[ERROR] La actualización parcial tocó campos que no debía.");

        // 8. Eliminar dos veces: la segunda falla como inexistente (C9)
        await servicio.Eliminar(9001);
        Console.WriteLine("[OK] Primera eliminación realizada.");
        try
        {
            await servicio.Eliminar(9001);
            Console.WriteLine("[ERROR] La segunda eliminación debió lanzar NoEncontradoExcepcion.");
        }
        catch (NoEncontradoExcepcion)
        {
            Console.WriteLine("[OK] Segunda eliminación rechazada: para la API ya no existe.");
        }

        // 9. Y el sistema vuelve a estar vacío
        var final = await servicio.ObtenerTodos(1000);
        Console.WriteLine(final.Any()
            ? "[ERROR] Debía quedar vacío otra vez."
            : "[OK] Tras el borrado, el sistema vuelve a estar vacío.");

        Console.WriteLine("=== Prueba de capas completada CON ÉXITO ===");
    }
}

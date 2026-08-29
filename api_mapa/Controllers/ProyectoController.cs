using ApiMapa.Excepciones;
using ApiMapa.Modelos;
using ApiMapa.Peticiones;
using ApiMapa.Repositorios;
using ApiMapa.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiMapa.Controllers;

/// <summary>
/// La capa 1: HTTP. No contiene lógica de negocio ni SQL (Artículo 3). Traduce las
/// excepciones del negocio a códigos de estado, y las peticiones a lo que la capa 2
/// entiende (3_plan.md §4.7).
///
/// La ruta se escribe COMPLETA y no con [controller]: ese token generaría "Proyecto"
/// con mayúscula y el contrato pide /api/proyecto (Artículo 10).
/// </summary>
[ApiController]
[Route("api/proyecto")]
public class ProyectoController : ControllerBase
{
    private readonly IServicioProyecto _servicio;

    public ProyectoController(IServicioProyecto servicio)
    {
        _servicio = servicio;
    }

    /// <summary>RF1 — Listar proyectos activos.</summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] int limite = 1000)
    {
        try
        {
            var datos = await _servicio.ObtenerTodos(limite);
            var lista = datos.ToList();

            // Vacío NO es error: 204 sin cuerpo. Es además el estado inicial del
            // sistema, porque la tabla arranca sin datos (C5).
            if (lista.Count == 0)
            {
                return NoContent();
            }

            return Ok(new
            {
                tabla = "proyecto",
                limite = limite,
                total = lista.Count,
                datos = lista
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { estado = 400, mensaje = "Parámetros inválidos.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF2 — Obtener un proyecto por su código.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        try
        {
            var proyecto = await _servicio.ObtenerPorId(id);
            return Ok(proyecto);
        }
        catch (NoEncontradoExcepcion ex)
        {
            return NotFound(new { estado = 404, mensaje = "Proyecto no encontrado.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF3 — Crear un proyecto.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ProyectoCrear peticion)
    {
        try
        {
            // El controlador traduce: la capa 2 recibe la entidad, no el cuerpo HTTP
            var proyecto = new Proyecto
            {
                Id = peticion.Id!.Value,
                Titulo = peticion.Titulo,
                Resumen = peticion.Resumen,
                Presupuesto = peticion.Presupuesto!.Value,
                TipoFinanciacion = peticion.TipoFinanciacion,
                TipoFondos = peticion.TipoFondos,
                FechaInicio = peticion.FechaInicio!.Value,
                FechaFin = peticion.FechaFin
            };

            await _servicio.Crear(proyecto);
            return Ok(new { estado = 200, mensaje = "Proyecto creado exitosamente." });
        }
        catch (SqlException ex)
        {
            // Código duplicado: la llave la defiende la base, no la API (C11)
            return StatusCode(500, new { estado = 500, mensaje = "Error al insertar en la base de datos.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF4 — Reemplazar completamente un proyecto.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Reemplazar(int id, [FromBody] ProyectoReemplazo peticion)
    {
        try
        {
            var proyecto = new Proyecto
            {
                Id = id,
                Titulo = peticion.Titulo,
                Resumen = peticion.Resumen,
                Presupuesto = peticion.Presupuesto!.Value,
                TipoFinanciacion = peticion.TipoFinanciacion,
                TipoFondos = peticion.TipoFondos,
                FechaInicio = peticion.FechaInicio!.Value,
                FechaFin = peticion.FechaFin
            };

            var filas = await _servicio.Reemplazar(id, proyecto);
            return Ok(new { estado = 200, mensaje = "Proyecto reemplazado.", filasAfectadas = filas });
        }
        catch (NoEncontradoExcepcion ex)
        {
            return NotFound(new { estado = 404, mensaje = "Proyecto no encontrado.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF5 — Actualizar parcialmente un proyecto.</summary>
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> ActualizarParcial(int id, [FromBody] ProyectoActualizar peticion)
    {
        try
        {
            var campos = new ProyectoCampos(
                peticion.Titulo, peticion.Resumen, peticion.Presupuesto,
                peticion.TipoFinanciacion, peticion.TipoFondos,
                peticion.FechaInicio, peticion.FechaFin);

            var filas = await _servicio.ActualizarParcial(id, campos);
            return Ok(new { estado = 200, mensaje = "Proyecto actualizado.", filasAfectadas = filas });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { estado = 400, mensaje = "Parámetros inválidos.", detalle = ex.Message });
        }
        catch (NoEncontradoExcepcion ex)
        {
            return NotFound(new { estado = 404, mensaje = "Proyecto no encontrado.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF6 — Borrado lógico.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var filas = await _servicio.Eliminar(id);
            return Ok(new { estado = 200, mensaje = "Proyecto eliminado.", filasAfectadas = filas });
        }
        catch (NoEncontradoExcepcion ex)
        {
            return NotFound(new { estado = 404, mensaje = "Proyecto no encontrado.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }
}

using System.Net.Http.Json;
using System.Text.Json;

namespace FrontMapa.Servicios;

/// <summary>
/// Proyecto, tal como el front lo maneja.
///
/// **Es una clase del front, no de la API.** Se parece a la de allá porque el
/// contrato es el mismo, y aun así son dos clases distintas en dos proyectos
/// distintos: lo único que los une es el JSON.
/// </summary>
public class Proyecto
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Resumen { get; set; } = string.Empty;

    public double Presupuesto { get; set; }

    public string TipoFinanciacion { get; set; } = string.Empty;

    public string TipoFondos { get; set; } = string.Empty;

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }
}

/// <summary>
/// Lo que devuelve cada operación: si salió bien, qué trajo, y qué errores hay
/// que mostrar. Existe para que las páginas **no vean códigos de estado**.
/// </summary>
public record Resultado<T>(bool Ok, T? Datos, List<string> Errores)
{
    public static Resultado<T> Bien(T datos) => new(true, datos, new());
    public static Resultado<T> Mal(List<string> errores) => new(false, default, errores);
}

/// <summary>
/// ==========================================================================
/// LA CAPA DE DATOS DEL FRONT
/// ==========================================================================
///
/// Es al front lo que el repositorio es a la API: la ÚNICA pieza que sabe
/// dónde viven los datos —en la API, nunca en la base— y la única que habla
/// HTTP.
///
/// **Y es específico de `proyecto`, no «de cualquier tabla».** Podría
/// escribirse un `ApiService.Listar("proyecto")` que sirviera para
/// todas, y sería más corto. No se hace: un método `Listar(string tabla)` no
/// le dice a nadie qué recursos existen, y el compilador deja de revisar si
/// esa tabla es una de las que hay (sección 6.1 de la metodología).
///
/// Cuando el proyecto tenga más recursos habrá un servicio por cada uno. Se
/// van a parecer mucho — y cada uno va a decir sus campos, sus mensajes y sus
/// operaciones, que es lo que un molde único borra.
/// </summary>
public class ServicioProyecto
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _opciones = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<string> NoDisponible = new()
    {
        "El servicio no está disponible. ¿Está arriba la API?"
    };

    public ServicioProyecto(HttpClient http)
    {
        _http = http;
    }

    // ------------------------------------------------------------------
    // Listar
    // ------------------------------------------------------------------
    public async Task<Resultado<List<Proyecto>>> Listar(int limite = 1000)
    {
        try
        {
            var r = await _http.GetAsync($"/api/proyecto?limite={limite}");

            // 204 es «no hay ninguno», y NO es un error: la pantalla muestra un
            // recuadro que lo dice, no un aviso rojo.
            if (r.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return Resultado<List<Proyecto>>.Bien(new());
            }

            if (!r.IsSuccessStatusCode)
            {
                return Resultado<List<Proyecto>>.Mal(await Mensajes(r));
            }

            // El sobre del contrato: { tabla, limite, total, datos[] }
            var sobre = await r.Content.ReadFromJsonAsync<JsonElement>();
            var datos = sobre.GetProperty("datos")
                .Deserialize<List<Proyecto>>(_opciones) ?? new();

            return Resultado<List<Proyecto>>.Bien(datos);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return Resultado<List<Proyecto>>.Mal(NoDisponible);
        }
    }

    // ------------------------------------------------------------------
    // Obtener uno
    // ------------------------------------------------------------------
    public async Task<Resultado<Proyecto>> Obtener(int llave)
    {
        try
        {
            var r = await _http.GetAsync($"/api/proyecto/{llave}");
            if (!r.IsSuccessStatusCode)
            {
                return Resultado<Proyecto>.Mal(await Mensajes(r));
            }

            var ficha = await r.Content.ReadFromJsonAsync<Proyecto>(_opciones);
            return Resultado<Proyecto>.Bien(ficha!);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return Resultado<Proyecto>.Mal(NoDisponible);
        }
    }

    // ------------------------------------------------------------------
    // Crear
    // ------------------------------------------------------------------
    public async Task<Resultado<bool>> Crear(Proyecto entidad)
    {
        return await Enviar(HttpMethod.Post, "/api/proyecto", entidad);
    }

    // ------------------------------------------------------------------
    // Reemplazar: «guardar la ficha completa»
    //
    // El código NO va en el cuerpo: identifica la fila y viaja en la ruta.
    // ------------------------------------------------------------------
    public async Task<Resultado<bool>> Reemplazar(int llave, Proyecto entidad)
    {
        var cuerpo = new Dictionary<string, object?>
        {
            [""] = null
        };
        cuerpo.Clear();
        cuerpo["titulo"] = entidad.Titulo;
        cuerpo["resumen"] = entidad.Resumen;
        cuerpo["presupuesto"] = entidad.Presupuesto;
        cuerpo["tipoFinanciacion"] = entidad.TipoFinanciacion;
        cuerpo["tipoFondos"] = entidad.TipoFondos;
        cuerpo["fechaInicio"] = entidad.FechaInicio;
        cuerpo["fechaFin"] = entidad.FechaFin;

        return await Enviar(HttpMethod.Put, $"/api/proyecto/{llave}", cuerpo);
    }

    // ------------------------------------------------------------------
    // Actualizar: «guardar solo lo que cambié»
    //
    // Solo viaja lo diligenciado. Un campo en blanco NO se envía —no es que se
    // envíe vacío: sencillamente no va— y la API lo deja como estaba.
    // ------------------------------------------------------------------
    public async Task<Resultado<bool>> Actualizar(int llave, string? titulo, string? resumen, double presupuesto, string? tipofinanciacion, string? tipofondos, DateTime? fechainicio, DateTime? fechafin)
    {
        var cuerpo = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(titulo)) cuerpo["titulo"] = titulo;
        if (!string.IsNullOrWhiteSpace(resumen)) cuerpo["resumen"] = resumen;
        if (presupuesto != null) cuerpo["presupuesto"] = presupuesto;
        if (!string.IsNullOrWhiteSpace(tipofinanciacion)) cuerpo["tipoFinanciacion"] = tipofinanciacion;
        if (!string.IsNullOrWhiteSpace(tipofondos)) cuerpo["tipoFondos"] = tipofondos;
        if (fechainicio != null) cuerpo["fechaInicio"] = fechainicio;
        if (fechafin != null) cuerpo["fechaFin"] = fechafin;

        return await Enviar(HttpMethod.Patch, $"/api/proyecto/{llave}", cuerpo);
    }

    // ------------------------------------------------------------------
    // Retirar del uso (la API lo hace lógico: la fila no se borra)
    // ------------------------------------------------------------------
    public async Task<Resultado<bool>> Eliminar(int llave)
    {
        return await Enviar(HttpMethod.Delete, $"/api/proyecto/{llave}", null);
    }

    private async Task<Resultado<bool>> Enviar(HttpMethod metodo, string ruta, object? cuerpo)
    {
        try
        {
            var peticion = new HttpRequestMessage(metodo, ruta);
            if (cuerpo != null)
            {
                peticion.Content = JsonContent.Create(cuerpo);
            }

            var r = await _http.SendAsync(peticion);
            return r.IsSuccessStatusCode
                ? Resultado<bool>.Bien(true)
                : Resultado<bool>.Mal(await Mensajes(r));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return Resultado<bool>.Mal(NoDisponible);
        }
    }

    /// <summary>
    /// Traduce a texto los errores que produce ESTA API.
    ///
    /// El sobre es plano y tiene dos formas:
    ///   { estado, mensaje, detalle }   → 400, 404, 500
    ///   { estado, mensaje, errores[] } → cuando el cuerpo no cumple
    ///
    /// **Este método es el único sitio del front que conoce ese formato.**
    /// </summary>
    private static async Task<List<string>> Mensajes(HttpResponseMessage r)
    {
        try
        {
            var sobre = await r.Content.ReadFromJsonAsync<JsonElement>();

            if (sobre.TryGetProperty("errores", out var errores)
                && errores.ValueKind == JsonValueKind.Array
                && errores.GetArrayLength() > 0)
            {
                return errores.EnumerateArray()
                    .Select(x => x.ToString())
                    .Where(x => x.Length > 0)
                    .ToList();
            }

            var partes = new List<string>();
            if (sobre.TryGetProperty("mensaje", out var m)) partes.Add(m.ToString());
            if (sobre.TryGetProperty("detalle", out var d)) partes.Add(d.ToString());
            partes.RemoveAll(string.IsNullOrWhiteSpace);

            return partes.Count > 0
                ? partes
                : new List<string> { "No se pudo completar la operación." };
        }
        catch
        {
            // Un 500 puede devolver HTML en vez de JSON.
            return new List<string> { "No se pudo completar la operación." };
        }
    }
}

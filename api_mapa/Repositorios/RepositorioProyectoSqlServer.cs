using System.Data;
using ApiMapa.Modelos;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ApiMapa.Repositorios;

/// <summary>
/// La capa 3 contra SQL Server, con Dapper (Artículo 2): el SQL se escribe a mano,
/// queda a la vista y SIEMPRE va parametrizado (@parametro).
///
/// Todas las consultas filtran por activo = 1: el borrado es lógico (Artículo 6).
/// </summary>
public class RepositorioProyectoSqlServer : IRepositorioProyecto
{
    private readonly string _cadenaConexion;

    public RepositorioProyectoSqlServer(IConfiguration configuracion)
    {
        _cadenaConexion = configuracion.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'SqlServer'.");
    }

    private IDbConnection CrearConexion() => new SqlConnection(_cadenaConexion);

    // Los alias traducen los nombres de la tabla (snake_case) a los de la entidad
    // (PascalCase): Dapper mapea por nombre.
    private const string COLUMNAS = @"id AS Id, titulo AS Titulo, resumen AS Resumen,
            presupuesto AS Presupuesto, tipo_financiacion AS TipoFinanciacion,
            tipo_fondos AS TipoFondos, fecha_inicio AS FechaInicio, fecha_fin AS FechaFin";

    public async Task<IEnumerable<Proyecto>> ObtenerTodos(int limite)
    {
        using var conexion = CrearConexion();
        var sql = $@"
            SELECT TOP (@Limite) {COLUMNAS}
            FROM proyecto
            WHERE activo = 1
            ORDER BY id ASC";

        return await conexion.QueryAsync<Proyecto>(sql, new { Limite = limite });
    }

    public async Task<Proyecto?> ObtenerPorId(int id)
    {
        using var conexion = CrearConexion();
        // Un proyecto inactivo responde como inexistente (C9)
        var sql = $@"
            SELECT {COLUMNAS}
            FROM proyecto
            WHERE id = @Id AND activo = 1";

        return await conexion.QueryFirstOrDefaultAsync<Proyecto>(sql, new { Id = id });
    }

    public async Task Crear(Proyecto proyecto)
    {
        using var conexion = CrearConexion();
        const string sql = @"
            INSERT INTO proyecto (id, titulo, resumen, presupuesto, tipo_financiacion,
                                  tipo_fondos, fecha_inicio, fecha_fin, activo)
            VALUES (@Id, @Titulo, @Resumen, @Presupuesto, @TipoFinanciacion,
                    @TipoFondos, @FechaInicio, @FechaFin, 1)";

        await conexion.ExecuteAsync(sql, proyecto);
    }

    public async Task<int> Reemplazar(Proyecto proyecto)
    {
        using var conexion = CrearConexion();
        const string sql = @"
            UPDATE proyecto
            SET titulo = @Titulo, resumen = @Resumen, presupuesto = @Presupuesto,
                tipo_financiacion = @TipoFinanciacion, tipo_fondos = @TipoFondos,
                fecha_inicio = @FechaInicio, fecha_fin = @FechaFin
            WHERE id = @Id AND activo = 1";

        return await conexion.ExecuteAsync(sql, proyecto);
    }

    public async Task<int> ActualizarParcial(int id, ProyectoCampos campos)
    {
        using var conexion = CrearConexion();

        // El PATCH escribe solo lo que llegó, así que la consulta se compone.
        // OJO: lo que se compone son NOMBRES DE COLUMNA de una lista cerrada,
        // escrita aquí; los VALORES siempre viajan como @parametro (3_plan.md §4.8).
        var asignaciones = new List<string>();
        var parametros = new DynamicParameters();
        parametros.Add("Id", id);

        void Agregar(string columna, string parametro, object? valor)
        {
            if (valor == null) return;
            asignaciones.Add($"{columna} = @{parametro}");
            parametros.Add(parametro, valor);
        }

        Agregar("titulo", "Titulo", campos.Titulo);
        Agregar("resumen", "Resumen", campos.Resumen);
        Agregar("presupuesto", "Presupuesto", campos.Presupuesto);
        Agregar("tipo_financiacion", "TipoFinanciacion", campos.TipoFinanciacion);
        Agregar("tipo_fondos", "TipoFondos", campos.TipoFondos);
        Agregar("fecha_inicio", "FechaInicio", campos.FechaInicio);
        Agregar("fecha_fin", "FechaFin", campos.FechaFin);

        if (asignaciones.Count == 0) return 0;

        var sql = $"UPDATE proyecto SET {string.Join(", ", asignaciones)} WHERE id = @Id AND activo = 1";

        return await conexion.ExecuteAsync(sql, parametros);
    }

    public async Task<int> EliminarLogico(int id)
    {
        using var conexion = CrearConexion();
        // Borrado LÓGICO en una sola consulta: cero filas afectadas significa
        // "no existe o ya estaba inactivo", que es el 404 del contrato (D-v1-4).
        const string sql = @"
            UPDATE proyecto
            SET activo = 0
            WHERE id = @Id AND activo = 1";

        return await conexion.ExecuteAsync(sql, new { Id = id });
    }
}

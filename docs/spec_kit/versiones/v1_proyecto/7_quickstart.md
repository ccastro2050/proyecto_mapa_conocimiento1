# Quickstart — Versión 1: arranque y smoke test

## 1. Arranque

```powershell
docker compose up -d --build
```

| Qué | Dónde |
|---|---|
| API — diagnóstico | http://localhost:8076/ |
| Documentación interactiva | http://localhost:8076/swagger |
| Listado de proyectos | http://localhost:8076/api/proyecto |
| SQL Server (opcional) | `localhost,11473` · usuario `sa` |

> **¿La contraseña?** Está en el `docker-compose.yml`, a la vista: es la
> excepción declarada del Artículo 7 de la
> [constitución](../../1_constitution.md). **Para correr el sistema no
> hace falta.** En su proyecto de aula eso no se copia: ahí va en un
> `.env` fuera de git.

**Si cambia la contraseña:** `docker compose down -v` y volver a subir.
Sin el `-v`, el usuario `sa` sigue con la clave vieja dentro del volumen.

## 2. Smoke test

```powershell
# 1. La API responde
curl http://localhost:8076/

# 2. El sistema arranca VACÍO
curl -i http://localhost:8076/api/proyecto
#    → 204, sin cuerpo

# 3. Crear y listar
curl -X POST http://localhost:8076/api/proyecto `
  -H "Content-Type: application/json" -d '{"id":9001,"titulo":"Mapa de conocimiento institucional","resumen":"Proyecto para consolidar la produccion academica de la universidad.","presupuesto":85000000.50,"tipoFinanciacion":"Interna","tipoFondos":"Recurrentes","fechaInicio":"2026-02-01T00:00:00","fechaFin":null}'
curl http://localhost:8076/api/proyecto
#    → total: 1, y presupuesto vuelve como NÚMERO

# 4. El ciclo de los cinco verbos
curl -X PUT http://localhost:8076/api/proyecto/9001 `
  -H "Content-Type: application/json" -d '{"titulo":"Mapa de conocimiento institucional - fase II","resumen":"Segunda fase.","presupuesto":120000000.0,"tipoFinanciacion":"Mixta","tipoFondos":"Convocatoria","fechaInicio":"2026-02-01","fechaFin":"2027-12-15"}'
curl -X PATCH http://localhost:8076/api/proyecto/9001 `
  -H "Content-Type: application/json" -d '{"presupuesto":95000000.0}'
curl http://localhost:8076/api/proyecto/9001

# 4b. MISMO cuerpo, dos verbos
curl -i -X PUT http://localhost:8076/api/proyecto/9001 `
  -H "Content-Type: application/json" -d '{"titulo":"X","resumen":"Y","presupuesto":1000.0,"tipoFinanciacion":"Interna","fechaInicio":"2026-02-01"}'
#    → 422: al PUT le falta tipoFondos
curl -i -X PATCH http://localhost:8076/api/proyecto/9001 `
  -H "Content-Type: application/json" -d '{"titulo":"X","resumen":"Y","presupuesto":1000.0,"tipoFinanciacion":"Interna","fechaInicio":"2026-02-01"}'
#    → 200

# 5. El borrado es LÓGICO
curl -X DELETE http://localhost:8076/api/proyecto/9001
curl -i http://localhost:8076/api/proyecto        # → 204 otra vez
curl -i -X DELETE http://localhost:8076/api/proyecto/9001   # → 404

docker compose exec sqlserver bash -c '/opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d mapa_local `
  -Q "SELECT id, activo FROM proyecto"'
#    → 9001 | 0

# 6. Los TRES casos de 422, que son la lección de esta versión
curl -i -X POST http://localhost:8076/api/proyecto `
  -H "Content-Type: application/json" -d '{"titulo":"X","resumen":"Y","presupuesto":1000.0,"tipoFinanciacion":"Interna","fechaInicio":"2026-02-01"}{"id":9001,"titulo":"Mapa de conocimiento institucional","resumen":"Proyecto para consolidar la produccion academica de la universidad.","presupuesto":85000000.50,"tipoFinanciacion":"Interna","tipoFondos":"Recurrentes","fechaInicio":"2026-02-01T00:00:00","fechaFin":null}'
#    → 422: falta tipoFondos
curl -i -X POST http://localhost:8076/api/proyecto `
  -H "Content-Type: application/json" -d '{"id":9003,"titulo":"X","resumen":"Y","presupuesto":"mucho","tipoFinanciacion":"Interna","tipoFondos":"Propios","fechaInicio":"2026-02-01"}'
#    → 422: el presupuesto llegó como palabra
curl -i -X POST http://localhost:8076/api/proyecto `
  -H "Content-Type: application/json" -d '{"id":9004,"titulo":"X","resumen":"Y","presupuesto":1000.0,"tipoFinanciacion":"Interna","tipoFondos":"Propios","fechaInicio":"ayer"}'
#    → 422: "ayer" no es una fecha

# 7. La prueba de capas: sin base de datos
docker compose exec api-mapa dotnet run --project pruebas
```

## 3. Regresión

Primera versión: no hay nada anterior que probar. **Desde la v2**, esta
sección conserva los smokes de las versiones cerradas.

## 4. Si algo falla

| Síntoma | Causa probable |
|---|---|
| `Login failed for user 'sa'` | Se cambió la contraseña sin `docker compose down -v` |
| El listado responde 200 con `total: 0` en vez de 204 | El controlador no devuelve `NoContent()` con la lista vacía (RF1) |
| El presupuesto vuelve entre comillas | Se declaró como texto en el modelo: debe ser `double` |
| Un inactivo aparece en el listado | A alguna consulta le falta `WHERE activo = 1` |
| `bad interpreter: /bin/bash^M` | `db/init.sh` con finales de línea de Windows (`.gitattributes`) |


---

## El front: la otra mitad de la versión

`docker compose up -d --build` levanta **tres** contenedores, no dos:

| Qué | Dónde |
|---|---|
| **LA PANTALLA** (lo que ve el usuario) | <http://localhost:8077> |
| Proyectos | <http://localhost:8077/proyectos> |
| La API | <http://localhost:8076> |

### La prueba automática

```powershell
python pruebas_humo/humo_front.py
```

Comprueba que las pantallas responden, que **los datos que muestran son los que
dio la API**, que no aparece jerga, y —lo que importa— que **con la API apagada
la pantalla sigue en pie con su aviso**. La apaga y la vuelve a encender sola.

**Lo que esa prueba NO puede hacer:** Blazor Server manda los clics por una
conexión persistente, así que un guion no puede llenar el formulario. Eso queda
para el recorrido a mano.

### El recorrido a mano, que hace una persona

1. Abra <http://localhost:8077>. Entre a **Proyectos**: la
   barra de direcciones dice `/proyectos` — una dirección de verdad, no
   un molde.
2. **Agregue** una ficha. Aparece en la tabla.
3. **Agréguela otra vez**, con el mismo código. Sale un aviso rojo con el
   mensaje que mandó la API — y **el formulario conserva lo que usted escribió**.
4. **Edítela** y use **«Guardar solo lo que cambié»** dejando campos vacíos:
   guarda, y lo que dejó en blanco queda como estaba.
5. Ahora **«Guardar la ficha completa»** con un campo obligatorio vacío: se
   rechaza. *El mismo formulario, dos comportamientos.*
6. **Retírela.** Pide confirmación y desaparece. Pero la fila **sigue en la
   base**: el borrado es lógico.
7. **Apague la API** y recargue la pantalla:
   ```powershell
   docker compose stop api-mapa
   ```
   La pantalla sigue cargando, con su menú y su pie, y dice que el servicio no
   está disponible. **Eso es lo que demuestra que son dos procesos.** Vuelva a
   levantarla con `docker compose start api-mapa`.

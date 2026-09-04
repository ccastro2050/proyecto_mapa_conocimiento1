# Contratos HTTP — Versión 1: los 7 endpoints exactos

> Base: `http://localhost:8076` · Documentación en `/swagger`. Lo que este
> documento dice se cumple **al pie de la letra** (Artículo 9).

## 0. Convenciones globales

**Sobre de lectura:**

```json
{ "tabla": "proyecto", "limite": 1000, "total": 1, "datos": [ … ] }
```

**Sobre de error:**

```json
{ "estado": 422, "mensaje": "Datos inválidos.", "detalle": "…",
  "errores": ["El campo tipoFondos es obligatorio."] }
```

**Los nombres JSON van en camelCase** (`tipoFinanciacion`, `fechaInicio`,
`filasAfectadas`): es el comportamiento por defecto de ASP.NET Core, así
que no hay nada que configurar ni nada que se pueda configurar mal.

> La ruta es `/api/proyecto` —nombra la tabla— y el cuerpo usa
> `fechaInicio`, no `fecha_inicio`. **El JSON no es una ventana a la
> tabla.**

**Las fechas se envían `"2026-02-01"` y vuelven `"2026-02-01T00:00:00"`.**
No es un descuido: la columna es `DATE`, pero al pasar por .NET se
convierte en un `DateTime`, y un `DateTime` **siempre** carga una hora. Al
enviar se acepta la forma corta; al devolver sale la larga.

Que el documento diga exactamente esto —y no lo que uno *esperaría*— es la
regla del Artículo 9: **el contrato se escribe contra lo que el sistema
responde**, verificado, no contra lo que sonaba bonito. Quitar esa hora es
posible, pero cuesta un conversor y **nadie lo ha pedido**: queda anotado
para la versión que lo necesite.

**Catálogo de códigos:**

| Situación | Código |
|---|---|
| Lectura o escritura correcta | **200** |
| Lectura sin filas activas | **204** (sin cuerpo) |
| Regla de negocio rota (`limite` ≤ 0, `PATCH` sin campos) | **400** |
| Cuerpo inválido: falta un campo o **el tipo no corresponde** | **422** |
| El código no existe, o está inactivo | **404** |
| La base rechaza (llave duplicada) o falla | **500** |

## 1. `GET /` — Diagnóstico

```
GET /
→ 200 { "mensaje": "API Mapa de Conocimiento — módulo de proyectos",
        "version": "v1", "contratos": "/swagger" }
```

**Sin desenlaces de error, y a propósito:** no recibe parámetros ni
consulta la base.

## 2. `GET /api/proyecto[?limite=N]` — Listar

```
GET /api/proyecto
→ 204 (sin cuerpo)          ← el ESTADO INICIAL: no hay proyectos

…y una vez creado alguno:
→ 200 { "tabla":"proyecto", "limite":1000, "total":1, "datos":[ … ] }

→ 400 si limite <= 0
```

Devuelve **solo** las filas con `activo = 1`. El campo `activo` **no viaja
en la respuesta**.

## 3. `GET /api/proyecto/{id}` — Obtener uno

```
GET /api/proyecto/9001
→ 200 {"id":9001,"titulo":"Mapa de conocimiento institucional","resumen":"Proyecto para consolidar la produccion academica de la universidad.","presupuesto":85000000.50,"tipoFinanciacion":"Interna","tipoFondos":"Recurrentes","fechaInicio":"2026-02-01T00:00:00","fechaFin":null}

GET /api/proyecto/999999                       ← no existe
→ 404 { "estado":404, "mensaje":"Proyecto no encontrado.",
        "detalle":"No existe un proyecto con el código 999999." }
```

Fíjese en el `presupuesto`: vuelve como **número**, sin comillas. Y
`fechaFin` vuelve como `null` mientras el proyecto siga en curso.

## 4. `POST /api/proyecto` — Crear

```
POST /api/proyecto
body {"id":9001,"titulo":"Mapa de conocimiento institucional","resumen":"Proyecto para consolidar la produccion academica de la universidad.","presupuesto":85000000.50,"tipoFinanciacion":"Interna","tipoFondos":"Recurrentes","fechaInicio":"2026-02-01T00:00:00","fechaFin":null}
→ 200 { "estado":200, "mensaje":"Proyecto creado exitosamente." }

body sin "tipoFondos"
→ 422 { "estado":422, "mensaje":"Datos inválidos.",
        "errores":["El campo tipoFondos es obligatorio."] }

body {"presupuesto":"mucho", …}                 ← el tipo también es regla
→ 422

body {"fechaInicio":"ayer", …}                  ← tampoco es una fecha
→ 422

body {"id":9001, …}                             ← código duplicado (PK)
→ 500 con el error del motor en detalle
```

**Los tres 422 son la lección de esta versión:** falta un campo, un número
que llega como palabra, y una fecha que no es una fecha. Ninguno llega a
la base.

Los dos últimos traen un detalle que conviene mirar: cuando el tipo no
corresponde, **el cuerpo entero deja de leerse**, así que la respuesta
suma `"The peticion field is required."` y el mensaje del deserializador
—en inglés, porque lo escribe el framework y no nosotros—:

```json
{ "estado": 422, "mensaje": "Datos inválidos.",
  "errores": ["The peticion field is required.",
              "The JSON value could not be converted to System.Nullable`1[System.Double]. Path: $.presupuesto …"] }
```

Es lo que el sistema responde de verdad, y por eso queda escrito así. Un
mensaje en español para este caso se puede montar, pero **nadie lo ha
pedido**: sería anticipar (Artículo 1).

## 5. `PUT /api/proyecto/{id}` — Reemplazo COMPLETO

```
PUT /api/proyecto/9001
body {"titulo":"Mapa de conocimiento institucional - fase II",
      "resumen":"Segunda fase del proyecto.","presupuesto":120000000.0,
      "tipoFinanciacion":"Mixta","tipoFondos":"Convocatoria",
      "fechaInicio":"2026-02-01","fechaFin":"2027-12-15"}
→ 200 { "estado":200, "mensaje":"Proyecto reemplazado.", "filasAfectadas":1 }

body sin "tipoFondos"
→ 422

PUT /api/proyecto/999999
→ 404
```

**Los seis obligatorios lo siguen siendo**: reemplazar es poner todo de
nuevo. El `id` no va en el cuerpo.

## 6. `PATCH /api/proyecto/{id}` — Actualización PARCIAL

```
PATCH /api/proyecto/9001
body {"presupuesto":95000000.0}                ← solo lo que cambia
→ 200 { "estado":200, "mensaje":"Proyecto actualizado.", "filasAfectadas":1 }

body sin "tipoFondos" (el MISMO que el PUT rechazó)
→ 200                                           ← aquí es válido

body {}                                         ← nada que actualizar
→ 400 { "estado":400, "mensaje":"Parámetros inválidos.",
        "detalle":"No se envió ningún campo para actualizar." }
```

**Esta pareja es la lección del contrato:** el mismo cuerpo da 422 en `PUT`
y 200 en `PATCH`.

## 7. `DELETE /api/proyecto/{id}` — Eliminar (LÓGICO)

```
DELETE /api/proyecto/9001
→ 200 { "estado":200, "mensaje":"Proyecto eliminado.", "filasAfectadas":1 }

DELETE /api/proyecto/9001                      ← segunda vez: ya está inactivo
→ 404
```

**La fila no se borra:** queda con `activo = 0`. El listado **vuelve a
responder 204** y la fila sigue en la base — es el criterio 5.


---

## El contrato de la PANTALLA

Lo anterior es el contrato de la API con **cualquiera** que la consuma. Este es
el de la pantalla con **quien la usa**: son dos contratos distintos, porque el
front es *un* cliente de la API, no *el* cliente.

| Pantalla | Dirección | Qué ofrece |
|---|---|---|
| Inicio | <http://localhost:8077/> | La entrada, con el enlace |
| Proyectos | <http://localhost:8077/proyectos> | La tabla, «Agregar», «Editar» y «Retirar» |

**Cada pantalla tiene dirección propia**, no una con el nombre de la tabla como
parámetro (sección 6.1 de la metodología). Se puede guardar como marcador,
poner en el menú y mandar por correo.

### Qué pantalla llama a qué endpoint

| Lo que hace el usuario | Lo que manda el front |
|---|---|
| Abrir la pantalla | `GET /api/proyecto?limite=1000` |
| «Agregar» y guardar | `POST /api/proyecto` |
| «Guardar la ficha completa» | `PUT /api/proyecto/{llave}` |
| «Guardar solo lo que cambié» | `PATCH /api/proyecto/{llave}` con **solo** lo diligenciado |
| «Retirar», tras confirmar | `DELETE /api/proyecto/{llave}` |

### Cómo traduce el front los errores

El front **no repite** ninguna validación de la API: manda, y muestra lo que
vuelva.

| Lo que responde la API | Lo que ve el usuario |
|---|---|
| Un cuerpo inválido, con `errores[]` | Un aviso rojo por cada error, con el texto que mandó la API |
| 400 / 404 / 500 con `{mensaje, detalle}` | Un aviso rojo con esos dos textos |
| **204** (no hay filas) | Un recuadro que dice que todavía no hay ninguna. **Vacío no es error** |
| **La API no responde** | «El servicio no está disponible. ¿Está arriba la API?» |

> **La última fila es la que demuestra la arquitectura.** Con la API apagada la
> pantalla **sigue en pie** —cabecera, menú, pie— y muestra ese aviso **sin un
> solo dato**. Lo comprueba `pruebas_humo/humo_front.py`, que apaga la API a
> propósito y la vuelve a encender.

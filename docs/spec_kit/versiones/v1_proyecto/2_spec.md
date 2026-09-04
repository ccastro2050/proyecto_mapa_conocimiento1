# Especificación — Versión 1: `proyecto` + SQL Server

> **Versión 1** ([mapa](../0_mapa_versiones.md)) · La primera rebanada
> vertical del módulo Mapa de Conocimiento. Ante conflicto, manda la
> [constitución](../../1_constitution.md).

## 1. Propósito de la v1

Construir la API del catálogo de **proyectos de investigación** de punta a
punta: controlador, servicio, repositorio e interfaces, contra SQL Server y
en un solo comando.

La v1 no busca cubrir el módulo: busca **dejar el patrón montado y
verificado**.

## 2. Alcance

**Incluye**

- El CRUD completo de `proyecto`: listar (con límite), obtener por código,
  crear, reemplazar, actualizar parcialmente y eliminar.
- **Borrado lógico**: `DELETE` marca `activo = 0` y los listados filtran
  los inactivos (Artículo 6).
- Un endpoint de diagnóstico y la documentación interactiva en `/swagger`.
- La prueba de capas, sin base de datos.

**NO incluye** — y no se anticipa nada de esto (Artículo 1)

- Ninguna otra tabla de las 21.
- Los productos, los docentes y los puentes que cuelgan del proyecto: v2.
- Autenticación, JWT, roles ni usuarios: v3.
- Frontend, dashboard ni consultas multitabla: v4.
- **Reactivar** un registro inactivo.
- Validar que `fechaFin` sea posterior a `fechaInicio`: es una regla de
  negocio que nadie ha pedido (C7).

## 3. Requisitos funcionales

### RF1 — Listar proyectos (GET + query string)
`GET /api/proyecto` → 200 con el sobre `{tabla, limite, total, datos:[…]}`.
- Devuelve **solo los activos**.
- `limite` opcional (entero > 0; por defecto 1000).
- Sin filas activas → **204** sin cuerpo. **Es el estado inicial.**

### RF2 — Obtener por código
`GET /api/proyecto/{id}` → 200. Inexistente **o inactivo** → 404.

### RF3 — Crear
`POST /api/proyecto` con `{id, titulo, resumen, presupuesto,
tipoFinanciacion, tipoFondos, fechaInicio}` obligatorios y `fechaFin`
opcional.
- Nace con `activo = 1`.
- Código ya existente → 500.

### RF4 — Reemplazar (PUT)
`PUT /api/proyecto/{id}` con los seis obligatorios del cuerpo.
- Falta uno → 422. Devuelve `filasAfectadas`; inexistente → 404.

### RF5 — Actualizar parcialmente (PATCH)
Solo se modifican los campos enviados. Cuerpo vacío → 400.

### RF6 — Eliminar (borrado lógico)
`DELETE /api/proyecto/{id}` marca `activo = 0`. Inexistente o ya inactivo
→ 404. La fila **no desaparece**.

### RF7 — Diagnóstico
`GET /` → JSON con mensaje, versión (`"v1"`) y la ruta de los contratos.

## 4. Requisitos no funcionales

- **Un solo comando** (Artículo 4) · **tres capas con interfaces**
  (Artículo 3) · **SQL a mano y parametrizado** (Artículo 2) · **todo en
  español** (Artículo 8) · documentación en `/swagger`.

## 5. Criterios de aceptación

1. **Un solo comando.** `docker compose up -d --build` deja corriendo SQL
   Server —con la base y sus 21 tablas— y la API.
   `GET http://localhost:8076/` responde `"version":"v1"`.
2. **El sistema arranca vacío.** `GET /api/proyecto` responde **204**.
3. **Crear y listar.** Un `POST` válido responde 200; después,
   `GET /api/proyecto` responde **200 con `total: 1`**, y el presupuesto
   vuelve como **número**, no como texto.
4. **Ciclo de los cinco verbos.** `POST` crea el código `9001` → `PUT` lo
   reemplaza → `PATCH` le cambia solo `presupuesto` → `GET` lo confirma →
   `DELETE` lo desactiva, y un **segundo** `DELETE` responde **404**.
   Además, un `PUT` sin `tipoFondos` responde **422** mientras el **mismo
   cuerpo** por `PATCH` responde **200**.
5. **El borrado es lógico, y se verifica.** Tras el `DELETE` el listado
   vuelve a **204**, **y la fila sigue en la base** con `activo = 0`.
6. **La validación es la frontera, y los tipos también son regla.** Tres
   casos distintos responden **422** sin tocar la base: falta
   `tipoFondos`; `presupuesto` llega como texto (`"mucho"`); `fechaInicio`
   llega como algo que no es una fecha (`"ayer"`). Y un código duplicado
   responde **500**.
7. **Prueba de capas.** El proyecto `pruebas/` ejecuta el servicio con un
   **repositorio de mentiras** y todas sus verificaciones pasan **con SQL
   Server apagado**.

## 6. Clarificaciones

> La **compuerta 1** del método: las ambigüedades detectadas ANTES de
> planear, con su respuesta y su razón.

| # | La pregunta | La respuesta, con su razón | Dónde quedó |
|---|---|---|---|
| C1 | `area_conocimiento.id` es `INT` y los datos son códigos como `1A01` | **Mandan los datos: `VARCHAR(6)`.** Si no, el script no carga su catálogo. Arrastra a `ac_proyecto` | `db/mapa_conocimiento.sql` |
| C2 | `disciplina` es `VARCHAR(60)` y su valor más largo tiene **124** | **Se agranda a `VARCHAR(150)`** | `db/mapa_conocimiento.sql` |
| C3 | `area_aplicacion.nombre` es `VARCHAR(60)` y su valor más largo tiene **129** | **Se agranda a `VARCHAR(150)`** | `db/mapa_conocimiento.sql` |
| C4 | Ninguna tabla del módulo trae `activo` | **Se agrega `activo BIT NOT NULL DEFAULT 1`** a las 18 del módulo | Artículo 6 · RF6 |
| C5 | El catálogo trae **"Cienias Naturales"** en 48 de las 218 filas | **Se corrige**: es un error de digitación de la fuente | `db/mapa_conocimiento.sql` |
| C6 | `proyecto` arranca **sin una sola fila**: el Excel no trae proyectos | **No es un problema: es una ventaja.** El smoke test recorre el ciclo desde el estado inicial y ejercita el **204 del listado vacío**, que una tabla llena nunca deja probar | RF1 · criterios 2 y 5 |
| C7 | `fechaFin` admite nulos y `fechaInicio` no. ¿Se valida que la final sea posterior? | **No en la v1.** Un proyecto en curso no tiene fecha de fin —por eso es opcional—, y comparar las dos es una regla de negocio que nadie pidió. Aquí sí se podría, porque son `DATE` de verdad: queda anotado para la versión que la necesite | `5_data_model` §2 |
| C8 | `presupuesto` es `FLOAT`. ¿Se acepta como texto en el JSON? | **No: llega como número o es 422.** El tipo también es regla, y aceptarlo como texto abriría la puerta a `"mucho"` | RF3 · criterio 6 |
| C9 | Un registro inactivo, ¿se puede consultar por su código? | **No: responde 404.** Si el listado los filtra, individualmente tampoco existen | RF2 · RF6 |
| C10 | ¿Y un segundo `DELETE`? | **404**, por consecuencia de C9 | RF6 · criterio 4 |
| C11 | `?limite=0` o negativo, ¿422 o 400? | **400.** La forma del dato es correcta; lo que se rompe es una regla de negocio | RF1 · Artículo 10 |
| C12 | Crear con un código que ya existe, ¿409 o 500? | **500.** En la v1 la llave la defiende la base | RF3 · criterio 6 |

## 7. Definición de TERMINADA

1. Los **7 criterios** pasan, verificados con el smoke test de
   [7_quickstart.md](7_quickstart.md) **corrido por una persona**.
2. La lista de [9_checklist.md](9_checklist.md) está en verde y firmada.
3. No queda ningún `[NECESITA ACLARACIÓN: …]`.
4. Se hace commit y **tag `v1`**.


---

## La PANTALLA — el requisito que completa la versión

Los requisitos anteriores describen la API. **Este describe lo que ve quien la
usa**, y sin él la versión no está cerrada: una versión que responde por HTTP
pero no se puede usar es media versión (Artículo 1.1).

En `http://localhost:8077/proyectos`:

| Lo que se puede hacer | Cómo se ve |
|---|---|
| **Consultar** | Una tabla con sus columnas: Código · Título · Resumen · Presupuesto… |
| **Agregar** una ficha | Un formulario con **un** botón |
| **Corregir** una ficha | El mismo formulario con **dos**: «Guardar la ficha completa» y «Guardar solo lo que cambié» |
| **Retirar** una ficha | Un botón que pide confirmación primero |

**Tres reglas de esta pantalla**, y las tres se comprueban:

1. **No le habla al usuario en jerga.** Ni «PUT», ni «422», ni rutas de la API.
   Los botones se llaman como el usuario piensa; que uno mande un reemplazo y
   el otro una modificación parcial es asunto del programa.
2. **Un error no pierde lo escrito.** Si la API rechaza el guardado, el
   formulario vuelve con lo que la persona había digitado.
3. **Vacío no es error.** Sin filas, la pantalla muestra un recuadro que lo
   dice, no un aviso rojo.

> **Los dos botones son la lección del contrato hecha pantalla.** El mismo
> formulario a medio llenar que «la ficha completa» rechaza, «solo lo que
> cambié» lo guarda. La diferencia no la decide ningún `if`: la decide **qué se
> envía**.

### Criterios de aceptación de la pantalla

| # | Criterio | Cómo se comprueba |
|---|---|---|
| P1 | La pantalla muestra las filas **que dio la API** | Se le piden a la API y se buscan en el texto visible de la pantalla |
| P2 | El ciclo completo se hace **desde la pantalla**, sin Swagger ni `curl` | Recorrido a mano de `7_quickstart.md` |
| P3 | **No hay jerga** en el texto visible | Se quitan las etiquetas HTML y se busca |
| P4 | **Son dos procesos**: con la API apagada la pantalla sigue en pie, con su aviso y **sin un solo dato** | `docker compose stop api-mapa` |

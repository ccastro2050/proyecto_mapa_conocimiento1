# Mapa de versiones — Módulo Mapa de Conocimiento

> La ruta completa del proyecto. Cada versión se especifica **solo cuando
> la anterior está cerrada** (commit + tag). Este mapa da la dirección; el
> spec kit de cada versión da el detalle.
>
> La ruta es la que define
> [modulo_mapa_conocimiento.md](../../../ProyectosDeAula/docs/modulo_mapa_conocimiento.md);
> aquí no se inventa nada, se ordena.

## La estrategia: back y front EN PARALELO

**Cada versión entrega su parte de la API *y* su parte del front**, y una
versión **no está cerrada** si la API responde y la pantalla no.

El front es **Blazor Server** sobre .NET 10, en un tercer contenedor, en el
puerto **8077**. Habla con la API solo por HTTP: no tiene cadena
de conexión, ni driver de base de datos, ni servicio `sqlserver` en su
`depends_on`.

Está exigido por el **Artículo 1.1** de la [constitución](../1_constitution.md).

## La ruta

| Versión | Qué agrega (acumulativo) | Estado |
|---|---|---|
| **v1** | CRUD completo de las **tablas sin clave foránea**, con los catálogos del Excel cargados | **En curso** ([spec](v1_proyecto/2_spec.md)) |
| v2 | CRUD de las **10 tablas con clave foránea**: las FK como listas desplegables cargadas desde la API, y validación de integridad referencial | Sin especificar |
| v3 | **JWT**, sesiones y control de acceso por roles; CRUD de `usuario`, `rol` y `rol_usuario` solo para administradores | Sin especificar |
| v4 | **10 consultas multitabla** (4+ tablas cada una), dashboard con gráficos, páginas corporativas, responsive/PWA y **publicación** en un servidor | Sin especificar |

## Qué tabla entra en qué versión

Las 21 tablas de la base, repartidas:

| Versión | Tablas |
|---|---|
| **v1** | `proyecto` · `area_conocimiento` · `objetivo_desarrollo_sostenible` · `area_aplicacion` · `termino_clave` · `linea_investigacion` · `aliado` · `tipo_producto` |
| v2 | `docente` · `producto` · `desarrolla` · `docente_producto` · `aliado_proyecto` · `palabras_clave` · `ac_proyecto` · `proyecto_linea` · y los demás puentes |
| v3 | `rol` · `usuario` · `rol_usuario` |

> **Ojo:** las 21 tablas **existen en la base desde la v1** (Artículo 5 de
> la [constitución](../1_constitution.md)). Lo que reparte esta tabla es
> qué puede **nombrar el código** de cada versión, no qué existe en el
> motor.

## Lo que este ejemplo construye

La v1 de este repositorio se construye sobre **`proyecto`**: una rebanada
vertical completa —controlador, servicio, repositorio, interfaces,
peticiones y prueba sin base de datos— sobre la tabla **con más campos de
las ocho sin clave foránea** (ocho, frente a los seis de la siguiente).

Es además la más rica en **tipos** de los cuatro módulos del curso: tiene
fechas de verdad (`DATE`, no texto), un valor numérico con decimales
(`FLOAT` para el presupuesto) y un campo opcional (`fecha_fin`, porque un
proyecto en curso no la tiene). Eso hace que el 422 por tipo equivocado sea
demostrable con tres casos distintos, no con uno.

Arranca **vacía**: el Excel de referencia no trae proyectos. Eso da un
smoke test que recorre el ciclo completo desde el estado inicial —**204 →
crear → total 1 → borrar → 204 otra vez**— y ejercita el 204 del listado
vacío.

Las demás tablas de la v1 son **ese mismo patrón** con otros nombres. El
equipo que tome este ejemplo lo revisa, y **si está de acuerdo lo retoma y
lo completa; si no, lo rehace a su manera**.

## Reglas del mapa

1. **No se anticipa nada de una versión futura** (Artículo 1 de la
   constitución): en la v1 no aparece una FK, ni un proyecto, ni un token.
2. **Una versión cerrada no se reabre**: los ajustes van en la siguiente.
3. **Regresión obligatoria**: al cerrar la vN, los criterios de todas las
   versiones anteriores deben seguir pasando.
4. El repositorio siempre muestra la **versión en curso, funcionando**.

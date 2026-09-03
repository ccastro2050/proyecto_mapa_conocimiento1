using FrontMapa.Components;
using FrontMapa.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server: el componente se renderiza en el servidor y el navegador
// recibe el HTML ya armado, manteniendo una conexión para los eventos.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ============================================================
// DE DÓNDE SALEN LOS DATOS
//
// De la API, por HTTP, y de ningún otro sitio. La dirección viene de la
// configuración: fuera de Docker vale lo de appsettings.json; dentro, el
// compose la sobreescribe con el NOMBRE del servicio —`api-mapa`—,
// porque `localhost` dentro de un contenedor es el contenedor mismo.
// ============================================================
var urlApi = builder.Configuration["UrlApi"] ?? "http://localhost:8076";

builder.Services.AddHttpClient<ServicioProyecto>(cliente =>
{
    cliente.BaseAddress = new Uri(urlApi);
    cliente.Timeout = TimeSpan.FromSeconds(10);
});

// ============================================================
// UN SERVICIO POR RECURSO (sección 6.1 de la metodología)
//
// Hoy hay uno porque la v1 construye una tabla. Cuando haya más recursos habrá
// más líneas aquí — no una que sirva para cualquier tabla.
// ============================================================

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

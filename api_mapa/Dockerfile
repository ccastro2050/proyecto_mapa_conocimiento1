# ==============================================================
# Dockerfile de la API Mapa de Conocimiento — ASP.NET Core / .NET 10 (puerto 8076)
#
# Usa la imagen del SDK (no la de runtime) a propósito: el contenedor corre
# `dotnet watch`, que RECOMPILA Y REINICIA solo cuando se guarda un .cs.
# ==============================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

# DOTNET_USE_POLLING_FILE_WATCHER: en Docker sobre Windows los avisos de "archivo
#   cambiado" no llegan al contenedor. Sin el sondeo, guardar un .cs no recompila
#   nada y parece que el hot-reload no existe.
# ASPNETCORE_URLS: dónde escucha adentro (el compose publica ese mismo 8076).
ENV DOTNET_USE_POLLING_FILE_WATCHER=1 \
    ASPNETCORE_URLS=http://0.0.0.0:8076

EXPOSE 8076

CMD ["dotnet", "watch", "run", "--non-interactive", "--project", "ApiMapa.csproj"]

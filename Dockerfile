# Ada-ncoa — container image for the web app.
# Built automatically by `docker compose up`.

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached until the project file changes).
COPY global.json ./
COPY src/DaMaiDeparte.Web/DaMaiDeparte.Web.csproj src/DaMaiDeparte.Web/
RUN dotnet restore src/DaMaiDeparte.Web/DaMaiDeparte.Web.csproj

COPY src/ src/
RUN dotnet publish src/DaMaiDeparte.Web/DaMaiDeparte.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Europe/Bucharest

COPY --from=build /app/publish .

# Source files may arrive with owner-only permissions; the app runs as a non-root user,
# so make everything world-readable (and directories traversable).
RUN chmod -R a+rX /app \
    # Folder for uploaded product photos (mounted as a volume); owned by the non-root app user.
    && mkdir -p /app/wwwroot/uploads/donations \
    && chown -R "$APP_UID" /app/wwwroot/uploads

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "DaMaiDeparte.Web.dll"]

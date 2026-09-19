#!/usr/bin/env bash
# One-time local setup for Ada-ncoa (macOS / Linux).
# Requires: .NET 10 SDK, Docker (or another SQL Server instance).
set -euo pipefail
cd "$(dirname "$0")/.."

WEB=src/DaMaiDeparte.Web
# user-secrets are only loaded in Development, also for the EF design-time host.
export ASPNETCORE_ENVIRONMENT=Development

if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
  echo "Set MSSQL_SA_PASSWORD first, e.g.: export MSSQL_SA_PASSWORD='Your_strong_Passw0rd'" >&2
  exit 1
fi

echo "==> Restoring tools and packages"
dotnet tool update --global dotnet-ef >/dev/null 2>&1 || dotnet tool install --global dotnet-ef
dotnet restore

echo "==> Storing the development connection string in user-secrets"
dotnet user-secrets --project "$WEB" set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=DaMaiDeparte_Dev;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;MultipleActiveResultSets=true"

echo "==> Building"
dotnet build

if ! ls "$WEB"/Data/Migrations/*_InitialCreate.cs >/dev/null 2>&1; then
  echo "==> Creating the InitialCreate migration"
  dotnet ef migrations add InitialCreate --project "$WEB" --output-dir Data/Migrations
fi

echo "==> Applying migrations"
dotnet ef database update --project "$WEB"

echo "Done. Start the app with: dotnet run --project $WEB --launch-profile https"

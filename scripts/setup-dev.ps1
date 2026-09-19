# One-time local setup for Ada-ncoa (Windows PowerShell).
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
$web = "src/DaMaiDeparte.Web"
$env:ASPNETCORE_ENVIRONMENT = "Development"  # user-secrets are only loaded in Development

if (-not $env:MSSQL_SA_PASSWORD -and -not $env:DMD_CONNECTION) {
    Write-Error "Set `$env:MSSQL_SA_PASSWORD (Docker SQL Server) or `$env:DMD_CONNECTION (full connection string) first."
}

$connection = if ($env:DMD_CONNECTION) { $env:DMD_CONNECTION } else {
    "Server=localhost,1433;Database=DaMaiDeparte_Dev;User Id=sa;Password=$($env:MSSQL_SA_PASSWORD);TrustServerCertificate=True;MultipleActiveResultSets=true"
}

dotnet tool update --global dotnet-ef 2>$null; if ($LASTEXITCODE -ne 0) { dotnet tool install --global dotnet-ef }
dotnet restore
dotnet user-secrets --project $web set "ConnectionStrings:DefaultConnection" $connection
dotnet build

if (-not (Get-ChildItem "$web/Data/Migrations/*_InitialCreate.cs" -ErrorAction SilentlyContinue)) {
    dotnet ef migrations add InitialCreate --project $web --output-dir Data/Migrations
}

dotnet ef database update --project $web
Write-Host "Done. Start the app with: dotnet run --project $web --launch-profile https"

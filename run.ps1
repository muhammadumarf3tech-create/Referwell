#!/usr/bin/env pwsh
# ReferWell — Single-command startup script
# Usage: .\run.ps1

Write-Host "`n🏥 ReferWell Startup Script`n" -ForegroundColor Cyan

# ── Step 1: Apply EF Core migrations ─────────────────────────────────────────
Write-Host "📦 Applying database migrations..." -ForegroundColor Yellow
Push-Location backend
dotnet ef database update `
    --project ReferWell.Infrastructure/ReferWell.Infrastructure.csproj `
    --startup-project ReferWell.Api/ReferWell.Api.csproj
Pop-Location

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Migration failed. Ensure SQL Server is running and the connection string in appsettings.json is correct." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Database ready." -ForegroundColor Green

# ── Step 2: Install frontend dependencies ────────────────────────────────────
Write-Host "`n📦 Installing frontend dependencies..." -ForegroundColor Yellow
Push-Location frontend
if (-not (Test-Path "node_modules")) {
    npm install
}
Pop-Location
Write-Host "✅ Frontend dependencies ready." -ForegroundColor Green

# ── Step 3: Launch backend and frontend concurrently ─────────────────────────
Write-Host "`n🚀 Starting servers...`n" -ForegroundColor Cyan
Write-Host "  Backend:  http://localhost:5165" -ForegroundColor Blue
Write-Host "  Frontend: http://localhost:4000" -ForegroundColor Blue
Write-Host "  Swagger:  http://localhost:5165/swagger`n" -ForegroundColor Blue

$backendJob = Start-Job -ScriptBlock {
    Set-Location "$using:PWD\backend"
    dotnet run --project ReferWell.Api/ReferWell.Api.csproj
}

$frontendJob = Start-Job -ScriptBlock {
    Set-Location "$using:PWD\frontend"
    npm run dev
}

Write-Host "✅ Both servers started. Press Ctrl+C to stop.`n" -ForegroundColor Green

try {
    while ($true) {
        $backendJob, $frontendJob | Receive-Job
        Start-Sleep -Seconds 2
    }
} finally {
    Stop-Job $backendJob, $frontendJob
    Remove-Job $backendJob, $frontendJob
    Write-Host "`n🛑 Servers stopped." -ForegroundColor Yellow
}

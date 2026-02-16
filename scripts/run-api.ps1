#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ApiDir = Join-Path $RootDir "src/MESNET.Presentation"

$Config = if ($args.Count -gt 0) { $args[0] } else { "Debug" }

Write-Host "=== MESNET API (Standalone) ===" -ForegroundColor Cyan
Write-Host "  Konfigürasyon : $Config"
Write-Host ""
Write-Host "  NOT: PostgreSQL ve RabbitMQ'nun zaten çalışıyor olması gerekir." -ForegroundColor DarkYellow
Write-Host "  Connection string'ler appsettings.Development.json'dan okunur."
Write-Host ""

Write-Host "[1/2] Build ediliyor..." -ForegroundColor Yellow
dotnet build "$RootDir/MESNET.slnx" -c $Config --no-incremental -q

Write-Host "[2/2] API başlatılıyor..." -ForegroundColor Yellow
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project $ApiDir -c $Config --no-build

#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$AppHostDir = Join-Path $RootDir "src/MESNET.AppHost"

# Podman container runtime
$env:ASPIRE_CONTAINER_RUNTIME = "podman"

$Config = if ($args.Count -gt 0) { $args[0] } else { "Debug" }

Write-Host "=== MESNET Aspire AppHost ===" -ForegroundColor Cyan
Write-Host "  Konfigürasyon : $Config"
Write-Host "  Container     : Podman"
Write-Host ""

# Podman kontrolü
if (-not (Get-Command podman -ErrorAction SilentlyContinue)) {
    Write-Host "HATA: Podman bulunamadı. Lütfen Podman'ı kurun." -ForegroundColor Red
    exit 1
}

try {
    podman info *>$null
} catch {
    Write-Host "HATA: Podman machine çalışmıyor. 'podman machine start' komutunu çalıştırın." -ForegroundColor Red
    exit 1
}

Write-Host "[1/2] Build ediliyor..." -ForegroundColor Yellow
dotnet build "$RootDir/MESNET.slnx" -c $Config --no-incremental -q

Write-Host "[2/2] Aspire AppHost başlatılıyor..." -ForegroundColor Yellow
dotnet run --project $AppHostDir -c $Config

#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ApiDir = Join-Path $RootDir "src/MESNET.Presentation"

$Action = if ($args.Count -gt 0) { $args[0] } else { "apply" }
$Config = if ($args.Count -gt 1) { $args[1] } else { "Debug" }

Write-Host "=== MESNET Marten Migration ===" -ForegroundColor Cyan
Write-Host "  Aksiyon        : $Action"
Write-Host "  Konfigürasyon  : $Config"
Write-Host ""

switch ($Action) {
    "apply" {
        Write-Host "Schema değişiklikleri uygulanıyor..." -ForegroundColor Yellow
        dotnet run --project $ApiDir -c $Config -- marten-apply
    }
    "assert" {
        Write-Host "Schema tutarlılığı kontrol ediliyor..." -ForegroundColor Yellow
        dotnet run --project $ApiDir -c $Config -- marten-assert
    }
    "patch" {
        Write-Host "SQL patch dosyası oluşturuluyor..." -ForegroundColor Yellow
        $MigrationsDir = Join-Path $RootDir "migrations"
        New-Item -ItemType Directory -Path $MigrationsDir -Force | Out-Null
        $Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $PatchFile = Join-Path $MigrationsDir "${Timestamp}_patch.sql"
        dotnet run --project $ApiDir -c $Config -- marten-patch $PatchFile
        Write-Host "Patch dosyası: $PatchFile" -ForegroundColor Green
    }
    "dump" {
        Write-Host "Mevcut schema dump ediliyor..." -ForegroundColor Yellow
        $MigrationsDir = Join-Path $RootDir "migrations"
        New-Item -ItemType Directory -Path $MigrationsDir -Force | Out-Null
        $DumpFile = Join-Path $MigrationsDir "schema_dump.sql"
        dotnet run --project $ApiDir -c $Config -- marten-dump $DumpFile
        Write-Host "Dump dosyası: $DumpFile" -ForegroundColor Green
    }
    default {
        Write-Host "Kullanım: migrate.ps1 {apply|assert|patch|dump} [Debug|Release]" -ForegroundColor White
        Write-Host ""
        Write-Host "  apply  — Schema değişikliklerini veritabanına uygula (varsayılan)"
        Write-Host "  assert — Mevcut schema'nın kod ile uyumlu olduğunu doğrula"
        Write-Host "  patch  — Fark SQL dosyası oluştur (migrations/ dizinine)"
        Write-Host "  dump   — Tüm schema'yı SQL olarak dışarı aktar"
        exit 1
    }
}

Write-Host ""
Write-Host "Tamamlandı." -ForegroundColor Green

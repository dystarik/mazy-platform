# Запуск: .\start.ps1

$ErrorActionPreference = "Stop"

function Write-Step { param($msg) Write-Host "`n$msg" -ForegroundColor Cyan }
function Write-Item { param($msg) Write-Host "  $msg" -ForegroundColor Gray }
function Write-Ok   { param($msg) Write-Host "  ✓ $msg" -ForegroundColor Green }
function Write-Fail { param($msg) Write-Host $msg -ForegroundColor Red }

$devDir  = Join-Path $PSScriptRoot "..\..\infra\dev"
$envFile = Join-Path $PSScriptRoot "..\..\infra\dev\.env"

if (-not (Test-Path $envFile)) {
    Write-Fail "Файл .env не найден: $envFile"
    exit 1
}

# ── 1. Найти все debug-файлы динамически ─────────────────────────────────────
$debugFiles = Get-ChildItem -Path $devDir -Filter "debug.*.yml" | Sort-Object Name

if ($debugFiles.Count -eq 0) {
    Write-Step "Debug-файлы не найдены. Запуск в полном режиме."
    $selectedFiles = @()
} else {
    Write-Step "Что запустить локально (через пробел), или Enter для полного запуска:"

    $index = 1
    $debugMap = @{}

    foreach ($file in $debugFiles) {
        $name = $file.BaseName -replace '^debug\.', ''
        Write-Item "[$index] $name"
        $debugMap[$index] = $file.Name
        $index++
    }

    $serviceInput = (Read-Host "`nВведите").Trim()

    $selectedFiles = @()

    if ($serviceInput -ne "") {
        $choices = $serviceInput -split '\s+'
        foreach ($choice in $choices) {
            if ($choice -match '^\d+$') {
                $num = [int]$choice
                if ($debugMap.ContainsKey($num)) {
                    $fileName = $debugMap[$num]
                    $selectedFiles += $fileName
                    Write-Ok $fileName
                } else {
                    Write-Host "  ! Неизвестный номер: $num" -ForegroundColor Yellow
                }
            }
        }
    }
}

# ── 2. Сборка ─────────────────────────────────────────────────────────────────
$files = @("-f", "docker-compose.yml")
foreach ($f in $selectedFiles) {
    $files += @("-f", $f)
}

Write-Step "Сборка образов..."
Set-Location $devDir
$buildArgs = @("compose") + $files + @("--env-file", $envFile, "build")
& docker @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Fail "Ошибка сборки"
    exit 1
}

# ── 3. Запуск ─────────────────────────────────────────────────────────────────
Write-Step "Запуск в фоне..."
$upArgs = @("compose") + $files + @("--env-file", $envFile, "up", "-d")

& docker @upArgs

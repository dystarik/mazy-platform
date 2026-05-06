# Запуск: .\publish-nuget.ps1
# Или с явным ключом: .\publish-nuget.ps1 -ApiKey "твой-ключ"

param(
    [string]$ApiKey,
    [string]$Configuration = "Release",
    [string]$Source = "https://api.nuget.org/v3/index.json"
)

$ErrorActionPreference = "Stop"

function Write-Step { param($msg) Write-Host "`n$msg" -ForegroundColor Cyan }
function Write-Item { param($msg) Write-Host "  → $msg"  -ForegroundColor Gray }
function Write-Ok   { param($msg) Write-Host $msg         -ForegroundColor Green }
function Write-Fail { param($msg) Write-Host $msg         -ForegroundColor Red }

$nupkgsDir   = Join-Path $PSScriptRoot "nupkgs"
$workspaceDir = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$librariesDir = Join-Path $workspaceDir "libraries"

# ── Подгрузка ключа из .env ───────────────────────────────────────────────────
if (-not $ApiKey) {
    $envFile = Join-Path $PSScriptRoot ".env"
    if (Test-Path $envFile) {
        $ApiKey = Get-Content $envFile |
            Select-String "NUGET_API_KEY=(.+)" |
            ForEach-Object { $_.Matches[0].Groups[1].Value }
        if ($ApiKey) { Write-Ok "Ключ загружен из .env" }
    }
}

if (-not $ApiKey) {
    Write-Fail "ApiKey не указан и не найден в .env (NUGET_API_KEY=...)"
    exit 1
}

try {
    # ── 1. Найти библиотеки ──────────────────────────────────────────────────
    $libraries = Get-ChildItem -Path $librariesDir -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName "src") } |
        Sort-Object Name

    if ($libraries.Count -eq 0) {
        Write-Fail "Библиотеки не найдены в $librariesDir"
        exit 1
    }

    # ── 2. Выбор библиотеки ──────────────────────────────────────────────────
    if ($libraries.Count -eq 1) {
        $library = $libraries[0]
        Write-Ok "Найдена библиотека: $($library.Name)"
    } else {
        Write-Step "Доступные библиотеки:"
        for ($i = 0; $i -lt $libraries.Count; $i++) {
            Write-Item "$($i + 1). $($libraries[$i].Name)"
        }

        $choice = [int](Read-Host "`nВыберите номер библиотеки")

        if ($choice -lt 1 -or $choice -gt $libraries.Count) {
            Write-Fail "Неверный выбор"
            exit 1
        }

        $library = $libraries[$choice - 1]
        Write-Ok "Выбрано: $($library.Name)"
    }

    # ── 3. Сборка ─────────────────────────────────────────────────────────────
    $projectPaths = Get-ChildItem -Path (Join-Path $library.FullName "src") -Filter "*.csproj" -Recurse |
        Select-Object -ExpandProperty FullName

    if ($projectPaths.Count -eq 0) {
        Write-Fail "Проекты не найдены в $($library.FullName)\src"
        exit 1
    }

    Write-Step "Сборка $($library.Name) ($Configuration)..."
    foreach ($projectPath in $projectPaths) {
        Write-Item (Split-Path $projectPath -Leaf)
        dotnet build $projectPath -c $Configuration
        if ($LASTEXITCODE -ne 0) { Write-Fail "Ошибка сборки"; exit 1 }
    }

    # ── 4. Подготовка папки для пакетов ──────────────────────────────────────
    if (Test-Path $nupkgsDir) { Remove-Item $nupkgsDir -Recurse -Force }
    New-Item -ItemType Directory -Path $nupkgsDir | Out-Null

    # ── 6. Упаковка ───────────────────────────────────────────────────────────
    Write-Step "Упаковка ($($projectPaths.Count) проектов)..."
    foreach ($projectPath in $projectPaths) {
        Write-Item (Split-Path $projectPath -Leaf)
        dotnet pack $projectPath -c $Configuration --no-build -o $nupkgsDir
        if ($LASTEXITCODE -ne 0) {
            Write-Fail "Ошибка упаковки: $(Split-Path $projectPath -Leaf)"
            exit 1
        }
    }

    # ── 7. Публикация ─────────────────────────────────────────────────────────
    $packages = Get-ChildItem -Path $nupkgsDir -Filter "*.nupkg"
    if ($packages.Count -eq 0) { Write-Fail "Пакеты не найдены"; exit 1 }

    Write-Step "Публикация ($($packages.Count) пакетов)..."
    foreach ($package in $packages) {
        Write-Item $package.Name
        dotnet nuget push $package.FullName --api-key $ApiKey --source $Source --skip-duplicate
        if ($LASTEXITCODE -ne 0) {
            Write-Fail "Ошибка публикации: $($package.Name)"
            exit 1
        }
    }

    Write-Ok "`nГотово!"
}
finally {
    if (Test-Path $nupkgsDir) { Remove-Item $nupkgsDir -Recurse -Force }
}

# Запуск: .\cleanup.ps1
# Опционально: .\cleanup.ps1 -DryRun  (показать что будет удалено, без удаления)

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-Step { param($msg) Write-Host "`n$msg" -ForegroundColor Cyan }
function Write-Item { param($msg) Write-Host "  → $msg"  -ForegroundColor Gray }
function Write-Ok   { param($msg) Write-Host $msg         -ForegroundColor Green }

$workspaceDir = Resolve-Path (Join-Path $PSScriptRoot "..\..")

# Папки которые удаляем
$targetFolders = @(
    # .NET
    "bin",
    "obj",
    # Node / Vue
    "node_modules",
    ".nuxt",
    ".output",
    "dist",
    # Прочее
    ".cache"
)

if ($DryRun) {
    Write-Host "`n[DryRun] Показываю что будет удалено, ничего не трогаю`n" -ForegroundColor Yellow
}

$totalSize = 0
$totalCount = 0

Write-Step "Поиск в $workspaceDir..."

foreach ($folder in $targetFolders) {
    $found = Get-ChildItem -Path $workspaceDir -Recurse -Directory -Filter $folder -ErrorAction SilentlyContinue |
        Where-Object {
            # Не удаляем если это папка внутри node_modules (вложенные node_modules)
            $_.FullName -notmatch "\\node_modules\\.+"
        }

    if ($found.Count -eq 0) { continue }

    Write-Step "/$folder ($($found.Count) папок):"

    foreach ($dir in $found) {
        $size = (Get-ChildItem $dir.FullName -Recurse -ErrorAction SilentlyContinue |
            Measure-Object -Property Length -Sum).Sum
        $sizeMb = [math]::Round($size / 1MB, 1)
        $totalSize += $size
        $totalCount++

        Write-Item "$($dir.FullName) ($sizeMb МБ)"

        if (-not $DryRun) {
            Remove-Item $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

$totalSizeMb = [math]::Round($totalSize / 1MB, 1)

if ($DryRun) {
    Write-Ok "`n[DryRun] Будет удалено: $totalCount папок, $totalSizeMb МБ"
} else {
    Write-Ok "`nУдалено: $totalCount папок, освобождено $totalSizeMb МБ"
}

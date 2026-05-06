param(
    [switch]$DryRun,
    [string]$WorkspacePath,
    [string]$Library
)

$ErrorActionPreference = "Stop"

$envPath = Join-Path $PSScriptRoot ".env"

function Read-Env {
    $map = @{}

    if (-not (Test-Path -LiteralPath $envPath)) {
        return $map
    }

    Get-Content -LiteralPath $envPath | ForEach-Object {
        $line = $_.Trim()

        if ($line -eq "" -or $line.StartsWith("#")) {
            return
        }

        if ($line -match "^([^=]+)=(.*)$") {
            $map[$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }

    return $map
}

function Resolve-WorkspacePath {
    param([hashtable]$Env)

    if (-not [string]::IsNullOrWhiteSpace($WorkspacePath)) {
        return [System.IO.Path]::GetFullPath($WorkspacePath)
    }

    if ($Env.ContainsKey("WORKSPACE_ROOT") -and -not [string]::IsNullOrWhiteSpace($Env["WORKSPACE_ROOT"])) {
        return [System.IO.Path]::GetFullPath($Env["WORKSPACE_ROOT"])
    }

    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
}

function Resolve-ConfiguredPaths {
    param(
        [string]$Workspace,
        [string]$ConfigValue,
        [string]$DefaultValue,
        [string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($ConfigValue)) {
        $ConfigValue = $DefaultValue
    }

    $paths = @()

    foreach ($item in ($ConfigValue -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ })) {
        $path = if ([System.IO.Path]::IsPathRooted($item)) {
            $item
        }
        else {
            Join-Path $Workspace $item
        }

        $fullPath = [System.IO.Path]::GetFullPath($path)

        if (-not (Test-Path -LiteralPath $fullPath)) {
            throw "$Name не найден: $fullPath"
        }

        $paths += $fullPath
    }

    return $paths | Sort-Object -Unique
}

function Get-VersionFromDirectoryBuildProps {
    param([string]$LibraryPath)

    $propsPath = Join-Path $LibraryPath "Directory.Build.props"

    if (-not (Test-Path -LiteralPath $propsPath)) {
        throw "Не найден Directory.Build.props: $propsPath"
    }

    [xml]$xml = Get-Content -LiteralPath $propsPath

    $version = $xml.Project.PropertyGroup.Version |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "В Directory.Build.props не найден <Version>"
    }

    return $version
}

function Get-PackageNamesFromLibrary {
    param([string]$LibraryPath)

    $srcPath = Join-Path $LibraryPath "src"

    if (-not (Test-Path -LiteralPath $srcPath)) {
        throw "Не найдена папка src: $srcPath"
    }

    $projects = Get-ChildItem -Path $srcPath -Filter "*.csproj" -Recurse -File

    if ($projects.Count -eq 0) {
        throw "В src не найдено *.csproj"
    }

    return $projects |
        ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) } |
        Sort-Object -Unique
}

function Update-PackageVersions {
    param(
        [string]$PropsPath,
        [string[]]$PackageNames,
        [string]$Version
    )

    [xml]$xml = Get-Content -LiteralPath $PropsPath
    $changed = $false

    foreach ($packageName in $PackageNames) {
        $node = $xml.Project.ItemGroup.PackageVersion |
            Where-Object { $_.Include -eq $packageName } |
            Select-Object -First 1

        if ($null -eq $node) {
            continue
        }

        if ($node.Version -ne $Version) {
            Write-Host "  ${packageName}: $($node.Version) -> $Version"

            if (-not $DryRun) {
                $node.Version = $Version
            }

            $changed = $true
        }
    }

    if ($changed -and -not $DryRun) {
        $xml.Save($PropsPath)
    }

    return $changed
}

$env = Read-Env
$workspacePath = Resolve-WorkspacePath -Env $env

if (-not (Test-Path -LiteralPath $workspacePath)) {
    throw "Рабочая область не найдена: $workspacePath"
}

$librariesRootName = if ($env.ContainsKey("LIBRARIES_ROOT") -and -not [string]::IsNullOrWhiteSpace($env["LIBRARIES_ROOT"])) {
    $env["LIBRARIES_ROOT"]
}
else {
    "libraries"
}

$librariesRoot = if ([System.IO.Path]::IsPathRooted($librariesRootName)) {
    [System.IO.Path]::GetFullPath($librariesRootName)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $workspacePath $librariesRootName))
}

if (-not (Test-Path -LiteralPath $librariesRoot)) {
    throw "Папка libraries не найдена: $librariesRoot"
}

$libraries = @()
if ($env.ContainsKey("LIBRARIES")) {
    $libraries = $env["LIBRARIES"] -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ }
}

if ($libraries.Count -eq 0) {
    $libraries = Get-ChildItem -Path $librariesRoot -Directory | Select-Object -ExpandProperty Name
}

$packageRoots = Resolve-ConfiguredPaths `
    -Workspace $workspacePath `
    -ConfigValue $env["SYNC_PACKAGE_ROOTS"] `
    -DefaultValue "services,edge" `
    -Name "SYNC_PACKAGE_ROOTS"

Write-Host "`nРабочая область: $workspacePath"
Write-Host "Библиотеки: $librariesRoot"
Write-Host "Корни для синка:"
$packageRoots | ForEach-Object { Write-Host "  $_" }

if (-not [string]::IsNullOrWhiteSpace($Library)) {
    $libraryName = $libraries |
        Where-Object { $_ -eq $Library } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($libraryName)) {
        throw "Библиотека не найдена в LIBRARIES: $Library"
    }
}
else {
    Write-Host "`nВыбери библиотеку:"
    for ($i = 0; $i -lt $libraries.Count; $i++) {
        Write-Host "[$($i + 1)] $($libraries[$i])"
    }

    $choice = Read-Host "`nНомер"
    $index = [int]$choice - 1

    if ($index -lt 0 -or $index -ge $libraries.Count) {
        throw "Неверный выбор"
    }

    $libraryName = $libraries[$index]
}
$libraryPath = Join-Path $librariesRoot $libraryName

if (-not (Test-Path -LiteralPath $libraryPath)) {
    throw "Библиотека не найдена: $libraryPath"
}

$version = Get-VersionFromDirectoryBuildProps $libraryPath
$packageNames = Get-PackageNamesFromLibrary $libraryPath

Write-Host "`nБиблиотека: $libraryName"
Write-Host "Версия: $version"
Write-Host "Пакеты:"
$packageNames | ForEach-Object { Write-Host "  $_" }

$propsFiles = foreach ($root in $packageRoots) {
    Get-ChildItem -Path $root -Recurse -File -Filter "Directory.Packages.props" |
        Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\|\\node_modules\\" }
}

$propsFiles = @($propsFiles | Sort-Object FullName -Unique)

if ($propsFiles.Count -eq 0) {
    throw "В корнях SYNC_PACKAGE_ROOTS не найдены Directory.Packages.props"
}

Write-Host "`nОбновление consumers..."
$changedFiles = 0

foreach ($propsFile in $propsFiles) {
    Write-Host "`n$($propsFile.FullName)"

    $changed = Update-PackageVersions `
        -PropsPath $propsFile.FullName `
        -PackageNames $packageNames `
        -Version $version

    if ($changed) {
        $changedFiles++
    }
    else {
        Write-Host "  Нет изменений"
    }
}

if ($DryRun) {
    Write-Host "`nDryRun завершён. Файлы не изменены. Найдено файлов: $($propsFiles.Count)"
}
else {
    Write-Host "`nГотово. Обновлено файлов: $changedFiles"
}

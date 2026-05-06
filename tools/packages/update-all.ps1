param(
    [switch]$DryRun,
    [switch]$IncludePrerelease,
    [switch]$AllowDowngrade,
    [switch]$ListTargets,
    [string]$Source,
    [string]$WorkspacePath
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

function Get-RegistrationBaseUrl {
    param([string]$ServiceIndexUrl)

    $serviceIndex = Invoke-RestMethod -Uri $ServiceIndexUrl

    $resources = @($serviceIndex.resources)

    $resource = $resources |
        Where-Object {
            @($_.'@type') -contains 'RegistrationsBaseUrl/3.6.0'
        } |
        Select-Object -First 1

    if ($null -eq $resource) {
        $resource = $resources |
            Where-Object {
                @($_.'@type') -contains 'RegistrationsBaseUrl/3.4.0'
            } |
            Select-Object -First 1
    }

    if ($null -eq $resource) {
        $resource = $resources |
            Where-Object {
                @($_.'@type') | Where-Object { $_ -like 'RegistrationsBaseUrl*' }
            } |
            Select-Object -First 1
    }

    if ($null -eq $resource) {
        throw "В service index не найден ресурс RegistrationsBaseUrl: $ServiceIndexUrl"
    }

    return $resource.'@id'.TrimEnd('/')
}

function Get-SemanticVersion {
    param([string]$Version)

    try {
        return [System.Management.Automation.SemanticVersion]::Parse($Version)
    }
    catch {
        return $null
    }
}

function Get-PackageVersionsFromFeed {
    param(
        [string]$PackageId,
        [string]$RegistrationBaseUrl
    )

    $lowerId = $PackageId.ToLowerInvariant()
    $indexUrl = "$RegistrationBaseUrl/$lowerId/index.json"
    $index = Invoke-RestMethod -Uri $indexUrl

    $result = New-Object System.Collections.Generic.List[object]

    foreach ($page in @($index.items)) {
        $entries = @()

        if ($page.items) {
            $entries = @($page.items)
        }
        elseif ($page.'@id') {
            $pageData = Invoke-RestMethod -Uri $page.'@id'
            $entries = @($pageData.items)
        }

        foreach ($entry in $entries) {
            $catalogEntry = $entry.catalogEntry

            if ($null -eq $catalogEntry) {
                continue
            }

            $result.Add([pscustomobject]@{
                Version         = $catalogEntry.version
                SemanticVersion = Get-SemanticVersion $catalogEntry.version
                Listed          = if ($null -eq $catalogEntry.listed) { $true } else { [bool]$catalogEntry.listed }
            })
        }
    }

    return $result
}

function Get-LatestPackageVersion {
    param(
        [string]$PackageId,
        [string]$CurrentVersion,
        [string]$RegistrationBaseUrl,
        [hashtable]$Cache
    )

    if (-not $Cache.ContainsKey($PackageId)) {
        $Cache[$PackageId] = Get-PackageVersionsFromFeed -PackageId $PackageId -RegistrationBaseUrl $RegistrationBaseUrl
    }

    $currentIsPrerelease = $CurrentVersion.Contains('-')

    $candidates = @($Cache[$PackageId]) | Where-Object { $_.Listed }

    if ($IncludePrerelease) {
        # Оставляем все версии как есть.
    }
    elseif ($currentIsPrerelease) {
        # Если пакет уже на prerelease-ветке, не скатываемся на старый stable.
        $candidates = @($candidates | Where-Object { $_.Version.Contains('-') })
    }
    else {
        $candidates = @($candidates | Where-Object { -not $_.Version.Contains('-') })
    }

    if ($candidates.Count -eq 0) {
        return $null
    }

    $withSemVer = @($candidates | Where-Object { $null -ne $_.SemanticVersion })
    if ($withSemVer.Count -gt 0) {
        return ($withSemVer | Sort-Object SemanticVersion | Select-Object -Last 1).Version
    }

    return ($candidates | Select-Object -Last 1).Version
}

function Update-DirectoryPackagesFile {
    param(
        [string]$PropsPath,
        [string]$RegistrationBaseUrl,
        [hashtable]$Cache
    )

    [xml]$xml = Get-Content -LiteralPath $PropsPath
    $changed = $false

    $packageNodes = @($xml.Project.ItemGroup.PackageVersion)

    foreach ($node in $packageNodes) {
        if ($null -eq $node -or [string]::IsNullOrWhiteSpace($node.Include) -or [string]::IsNullOrWhiteSpace($node.Version)) {
            continue
        }

        Write-Host "  Проверка $($node.Include) ($($node.Version))..."

        $latestVersion = Get-LatestPackageVersion `
            -PackageId $node.Include `
            -CurrentVersion $node.Version `
            -RegistrationBaseUrl $RegistrationBaseUrl `
            -Cache $Cache

        $currentSemVer = Get-SemanticVersion $node.Version
        $latestSemVer = Get-SemanticVersion $latestVersion

        if ([string]::IsNullOrWhiteSpace($latestVersion) -or $latestVersion -eq $node.Version) {
            Write-Host "    Без изменений"
            continue
        }

        if (-not $AllowDowngrade -and $null -ne $currentSemVer -and $null -ne $latestSemVer -and $latestSemVer -lt $currentSemVer) {
            Write-Host "    Пропуск: найденная версия ниже текущей ($latestVersion < $($node.Version))"
            continue
        }

        Write-Host "    Обновление: $($node.Version) -> $latestVersion"

        if (-not $DryRun) {
            $node.Version = $latestVersion
        }

        $changed = $true
    }

    if ($changed -and -not $DryRun) {
        $xml.Save($PropsPath)
    }

    return $changed
}

$env = Read-Env

if ([string]::IsNullOrWhiteSpace($Source)) {
    if ($env.ContainsKey("NUGET_SOURCE") -and -not [string]::IsNullOrWhiteSpace($env["NUGET_SOURCE"])) {
        $Source = $env["NUGET_SOURCE"]
    }
    else {
        $Source = "https://api.nuget.org/v3/index.json"
    }
}

$workspacePath = Resolve-WorkspacePath -Env $env

if (-not (Test-Path -LiteralPath $workspacePath)) {
    throw "Рабочая область не найдена: $workspacePath"
}

$packageRoots = Resolve-ConfiguredPaths `
    -Workspace $workspacePath `
    -ConfigValue $env["UPDATE_PACKAGE_ROOTS"] `
    -DefaultValue "services,edge,libraries" `
    -Name "UPDATE_PACKAGE_ROOTS"

$propsFiles = foreach ($root in $packageRoots) {
    Get-ChildItem -Path $root -Recurse -Filter "Directory.Packages.props" -File |
        Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\|\\node_modules\\" }
}

$propsFiles = @($propsFiles | Sort-Object FullName -Unique)

if ($propsFiles.Count -eq 0) {
    throw "Файлы Directory.Packages.props не найдены в UPDATE_PACKAGE_ROOTS"
}

Write-Host "Источник: $Source"
Write-Host "Рабочая область: $workspacePath"
Write-Host "IncludePrerelease: $IncludePrerelease"
Write-Host "AllowDowngrade: $AllowDowngrade"
Write-Host "Корни обновления:"
$packageRoots | ForEach-Object { Write-Host "  $_" }

if ($ListTargets) {
    Write-Host "`nБудут проверены файлы:"
    $propsFiles | ForEach-Object { Write-Host "  $($_.FullName)" }
    Write-Host "`nListTargets завершён. Сеть не использовалась, файлы не изменены. Найдено файлов: $($propsFiles.Count)"
    return
}

$registrationBaseUrl = Get-RegistrationBaseUrl -ServiceIndexUrl $Source
$cache = @{}
$changedFiles = 0

Write-Host "`nОбновление Directory.Packages.props..."

foreach ($propsFile in $propsFiles) {
    Write-Host "`n$($propsFile.FullName)"

    $changed = Update-DirectoryPackagesFile `
        -PropsPath $propsFile.FullName `
        -RegistrationBaseUrl $registrationBaseUrl `
        -Cache $cache

    if ($changed) {
        $changedFiles++
    }
    else {
        Write-Host "  Нет изменений"
    }
}

if ($DryRun) {
    Write-Host "`nDryRun завершён. Файлы не изменены. Проверено файлов: $($propsFiles.Count)"
}
else {
    Write-Host "`nГотово. Обновлено файлов: $changedFiles"
}

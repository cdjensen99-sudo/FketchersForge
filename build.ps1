param(
    [string]$ValheimPath = "D:\SteamLibrary\steamapps\common\Valheim",
    [string]$DeployProfile = "C:\Users\cdjen\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Testing",
    [switch]$Deploy,
    [switch]$Package
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\FletchersForge\FletchersForge.csproj"
$dll = Join-Path $root "artifacts\FletchersForge.dll"
$thunderstore = Join-Path $root "thunderstore"
$manifestPath = Join-Path $thunderstore "manifest.json"
$manifest = if (Test-Path $manifestPath) {
    Get-Content $manifestPath | ConvertFrom-Json
}
else {
    $null
}

dotnet build $project -p:ValheimPath=$ValheimPath -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built: $dll"

if ($Deploy) {
    $pluginDir = Join-Path $DeployProfile "BepInEx\plugins\Hardwire99-FletchersForge"
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
    $dest = Join-Path $pluginDir "FletchersForge.dll"

    try {
        Copy-Item $dll $dest -Force
        if (Test-Path (Join-Path $thunderstore "manifest.json")) {
            Copy-Item (Join-Path $thunderstore "manifest.json") (Join-Path $pluginDir "manifest.json") -Force
        }
        Write-Host "Deployed to $dest"
        Write-Host "Note: Icons are embedded in the DLL. build.ps1 does not copy Icons/ to the profile."
    }
    catch {
        $pending = Join-Path $pluginDir "FletchersForge.dll.pending"
        Copy-Item $dll $pending -Force
        Write-Warning "Game may have the plugin locked. Close Valheim and copy pending DLL."
    }
}

if ($Package) {
    if (-not $manifest) {
        throw "thunderstore\manifest.json is required for packaging."
    }

    $team = "Hardwire99"
    $artifactsDir = Join-Path $root "artifacts"
    $thunderstoreStaging = Join-Path $artifactsDir "thunderstore-staging"

    $packageName = "{0}-{1}-{2}.zip" -f $team, $manifest.name, $manifest.version_number
    $packagePath = Join-Path $artifactsDir $packageName

    if (Test-Path $thunderstoreStaging) {
        Remove-Item $thunderstoreStaging -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $thunderstoreStaging | Out-Null

    Copy-Item $dll (Join-Path $thunderstoreStaging "FletchersForge.dll") -Force
    Copy-Item (Join-Path $thunderstore "manifest.json") (Join-Path $thunderstoreStaging "manifest.json") -Force
    Copy-Item (Join-Path $thunderstore "README.md") (Join-Path $thunderstoreStaging "README.md") -Force

    $changelog = Join-Path $thunderstore "CHANGELOG.md"
    if (Test-Path $changelog) {
        Copy-Item $changelog (Join-Path $thunderstoreStaging "CHANGELOG.md") -Force
    }

    $iconSource = Join-Path $thunderstore "icon.png"
    if (Test-Path $iconSource) {
        Copy-Item $iconSource (Join-Path $thunderstoreStaging "icon.png") -Force
    }
    else {
        Write-Warning "thunderstore\icon.png is missing. Add a 256x256 PNG before uploading."
    }

    $iconsSource = Join-Path $thunderstore "Icons"
    if (Test-Path $iconsSource) {
        Copy-Item $iconsSource (Join-Path $thunderstoreStaging "Icons") -Recurse -Force
    }
    else {
        Write-Warning "thunderstore\Icons is missing. README gallery images will not display on Thunderstore."
    }

    if (Test-Path $packagePath) {
        Remove-Item $packagePath -Force
    }

    Compress-Archive -Path (Join-Path $thunderstoreStaging "*") -DestinationPath $packagePath -Force

    Write-Host "Package: $packagePath"
    Write-Host "Upload to Thunderstore and Hexium (same zip). Team $team, package '$($manifest.name)'."
}

if (-not $Deploy -and -not $Package) {
    Write-Host "Skipped deploy/package. Pass -Deploy and/or -Package as needed."
}

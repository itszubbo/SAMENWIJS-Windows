param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$windowsDir = $PSScriptRoot
$project = Join-Path $windowsDir "SAMENWIJS.Windows\SAMENWIJS.Windows.csproj"
$installer = Join-Path $windowsDir "Installer.wxs"
$icon = Join-Path $windowsDir "SAMENWIJS.Windows\Assets\samenwijs.ico"
$artifacts = Join-Path $windowsDir "artifacts"
$publishDir = Join-Path $artifacts "publish"
$msi = Join-Path $artifacts "SAMENWIJS-Setup.msi"

$dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "De .NET 10 SDK is niet gevonden. Installeer de .NET 10 SDK en start dit script opnieuw."
}
$sdks = & $dotnet.Source --list-sdks
if ($LASTEXITCODE -ne 0 -or -not ($sdks | Where-Object { $_ -match '^10\.' })) {
    throw "De .NET 10 SDK is niet gevonden. Installeer de .NET 10 SDK en start dit script opnieuw."
}

$wix = Get-Command "wix" -ErrorAction SilentlyContinue
if (-not $wix) {
    throw "WiX CLI is niet gevonden. Voer eerst uit: dotnet tool install --global wix --version 5.0.2"
}

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
& $dotnet.Source publish $project -c $Configuration -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "De Windows-app kon niet worden gepubliceerd."
}

$requiredFiles = @(
    (Join-Path $publishDir "SAMENWIJS.exe"),
    (Join-Path $publishDir "wwwroot\index.html")
)
foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file)) {
        throw "Vereist publicatiebestand ontbreekt: $file"
    }
}

$wixArgs = @(
    "build", $installer,
    "-arch", "x64",
    "-d", "PublishDir=$publishDir",
    "-d", "IconPath=$icon",
    "-out", $msi
)
& $wix.Source @wixArgs
if ($LASTEXITCODE -ne 0) {
    throw "De MSI-installer kon niet worden gemaakt."
}

Write-Host "Klaar: $msi"

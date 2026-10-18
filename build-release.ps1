<#
.SYNOPSIS
    Builds and packages the complete WebScraperSuite release package with all standalone Windows executables.

.DESCRIPTION
    Compiles all WinForms tools and the Webhook receiver into a unified self-contained win-x64
    distribution folder and creates a ready-to-upload ZIP archive for GitHub Releases.

.EXAMPLE
    .\build-release.ps1
#>

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "./dist/WebScraperPro",
    [string]$ZipArchive = "./dist/WebScraperSuite-Windows-x64.zip"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       WebScraperSuite - Release Build & Packager         " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Verify dotnet CLI is present
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "The 'dotnet' SDK command was not found on PATH. Please install .NET 9 SDK from https://dotnet.microsoft.com/download/dotnet/9.0."
}

# Clean or create output directory
if (Test-Path $OutputDir) {
    Write-Host "[1/6] Cleaning existing output directory..." -ForegroundColor Yellow
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$projects = @(
    @{ Name = "Scraper.Advanced.WinForms"; Path = "Scraper.Advanced.WinForms/Scraper.Advanced.WinForms.csproj" },
    @{ Name = "Scraper.Picker.WinForms";   Path = "Scraper.Picker.WinForms/Scraper.Picker.WinForms.csproj" },
    @{ Name = "Scraper.Media.WinForms";    Path = "Scraper.Media.WinForms/Scraper.Media.WinForms.csproj" },
    @{ Name = "Scraper.SiteCloner.WinForms"; Path = "Scraper.SiteCloner.WinForms/Scraper.SiteCloner.WinForms.csproj" },
    @{ Name = "Scraper.Automatic.WinForms"; Path = "Scraper.Automatic.WinForms/Scraper.Automatic.WinForms.csproj" },
    @{ Name = "Scraper.WebhookReceiver";   Path = "Scraper.WebhookReceiver/Scraper.WebhookReceiver.csproj" }
)

$step = 2
foreach ($proj in $projects) {
    Write-Host "[$step/8] Publishing $($proj.Name)..." -ForegroundColor Green
    dotnet publish $proj.Path `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        -p:PublishReadyToRun=true `
        -o $OutputDir
    $step++
}

# Copy main launcher as WebScraperPro.exe for user convenience
Write-Host "[$step/8] Creating convenience launcher 'WebScraperPro.exe'..." -ForegroundColor Green
Copy-Item "$OutputDir/Scraper.Advanced.WinForms.exe" "$OutputDir/WebScraperPro.exe" -Force
$step++

# Install Playwright browser dependencies if playwright script is present
if (Test-Path "$OutputDir/playwright.ps1") {
    Write-Host "[$step/8] Installing Playwright Chromium browser..." -ForegroundColor Green
    try {
        pwsh "$OutputDir/playwright.ps1" install chromium
    } catch {
        Write-Warning "Playwright install step encountered an issue. Users can still run 'playwright.ps1 install' manually."
    }
}
$step++

# Compress to ZIP
Write-Host "[$step/8] Compressing release package to $ZipArchive..." -ForegroundColor Green
if (Test-Path $ZipArchive) {
    Remove-Item $ZipArchive -Force
}
Compress-Archive -Path "$OutputDir/*" -DestinationPath $ZipArchive -Force

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Build succeeded!" -ForegroundColor Green
Write-Host " Output Directory: $OutputDir" -ForegroundColor White
Write-Host " Release Archive:  $ZipArchive" -ForegroundColor White
Write-Host " You can now upload '$ZipArchive' directly to your GitHub Release Assets!" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

#!/usr/bin/env pwsh
# Local testing script for Burner-CLI

param(
	[switch]$SkipInstall,
	[switch]$Uninstall,
	[switch]$Run
)

$ErrorActionPreference = "Stop"
$PackageId = "Burner-CLI"
$ProjectPath = "$PSScriptRoot/Burner"
$NupkgPath = "$PSScriptRoot/nupkg"

Write-Host "`n🔥 Burner-CLI Local Test Script`n" -ForegroundColor Yellow

# Uninstall only
if ($Uninstall) {
	Write-Host "Uninstalling $PackageId..." -ForegroundColor Cyan
	dotnet tool uninstall -g $PackageId 2>$null
	Write-Host "✅ Uninstalled`n" -ForegroundColor Green
	exit 0
}

# Run without reinstalling
if ($Run) {
	Write-Host "Running burner --help...`n" -ForegroundColor Cyan
	burner --help
	exit 0
}

if (-not $SkipInstall) {
	# Uninstall existing
	Write-Host "1️⃣  Uninstalling existing $PackageId (if any)..." -ForegroundColor Cyan
	dotnet tool uninstall -g $PackageId 2>$null
	Write-Host ""

	# Clean and pack
	Write-Host "2️⃣  Packing project..." -ForegroundColor Cyan
	if (Test-Path $NupkgPath) {
		Remove-Item -Recurse -Force $NupkgPath
	}
	dotnet pack $ProjectPath -c Release -o $NupkgPath
	Write-Host ""

	# Install from local
	Write-Host "3️⃣  Installing from local package..." -ForegroundColor Cyan
	dotnet tool install -g $PackageId --add-source $NupkgPath
	Write-Host ""
}

# Verify installation
Write-Host "4️⃣  Verifying installation..." -ForegroundColor Cyan
$toolPath = (Get-Command burner -ErrorAction SilentlyContinue).Source
if ($toolPath) {
	Write-Host "✅ Installed at: $toolPath" -ForegroundColor Green
}
else {
	Write-Host "❌ Tool not found in PATH" -ForegroundColor Red
	exit 1
}
Write-Host ""

# Run test commands
Write-Host "5️⃣  Running test commands...`n" -ForegroundColor Cyan

Write-Host ">>> burner --help" -ForegroundColor DarkGray
burner --help
Write-Host ""

Write-Host ">>> burner config" -ForegroundColor DarkGray
burner config
Write-Host ""

Write-Host ">>> burner templates" -ForegroundColor DarkGray
burner templates
Write-Host ""

Write-Host ">>> burner list" -ForegroundColor DarkGray
burner list
Write-Host ""

Write-Host "`n✅ Local testing complete!`n" -ForegroundColor Green
Write-Host "Try these commands:" -ForegroundColor Yellow
Write-Host "  burner new dotnet test-project" -ForegroundColor White
Write-Host "  burner list" -ForegroundColor White
Write-Host "  burner burn test-project" -ForegroundColor White
Write-Host ""

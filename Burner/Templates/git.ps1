#!/usr/bin/env pwsh
# Burner built-in template: Empty Git repository with README
# Environment: BURNER_NAME, BURNER_PATH, BURNER_DATED_NAME
# Working directory is already set to project path

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "git is required for the 'git' template"
    exit 1
}

git init | Out-Null
if ($LASTEXITCODE -ne 0) { exit 1 }

@"
# $env:BURNER_NAME
"@ | Set-Content "README.md"

#!/usr/bin/env pwsh
# Burner built-in template: .NET Console Application
# Environment: BURNER_NAME, BURNER_PATH, BURNER_DATED_NAME
# Working directory is already set to project path
dotnet new console -n $env:BURNER_NAME -o . --force
if ($LASTEXITCODE -ne 0) { exit 1 }

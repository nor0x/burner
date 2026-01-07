#!/bin/bash
# Burner built-in template: .NET Console Application
# Environment: BURNER_NAME, BURNER_PATH, BURNER_DATED_NAME
# Working directory is already set to project path
set -e
dotnet new console -n "$BURNER_NAME" -o . --force

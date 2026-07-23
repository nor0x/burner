#!/usr/bin/env pwsh
# Burner built-in template: Python Hello World
# Environment: BURNER_NAME, BURNER_PATH, BURNER_DATED_NAME
# Working directory is already set to project path

@"
# Python Burner Project: $env:BURNER_NAME

A tiny Python starter created with Burner.
"@ | Set-Content "README.md"

@"
print("Hello, world!")
"@ | Set-Content "main.py"

@"
.venv/
__pycache__/
*.pyc
"@ | Set-Content ".gitignore"

Set-Content "requirements.txt" ""

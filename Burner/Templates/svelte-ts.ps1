#!/usr/bin/env pwsh
# BURNER_INTERACTIVE
# svelte-ts.ps1 - Creates a Svelte TypeScript app using Vite

Set-Location $env:BURNER_PATH
npm create vite@latest . -- --template svelte-ts
npm install
npm pkg set name=$env:BURNER_NAME
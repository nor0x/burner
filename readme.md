# Burner 🔥

<p align="center">
<img src="Art/icon_alpha.png" alt="Burner Logo" width="170" style="margin-bottom:20px" />
</p>
<p align="center">
<strong>
Spark ideas. Burn 🔥 when done. It's the home 🏡 for burner projects.
</strong>

</p>
<p align="center">
<a href="https://github.com/nor0x/burner/actions/workflows/ci.yml"><img src="https://github.com/nor0x/burner/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
<a href="https://github.com/nor0x/burner/actions/workflows/publish.yml"><img src="https://github.com/nor0x/burner/actions/workflows/publish.yml/badge.svg" alt="Publish NuGet" /></a>
<a href="https://www.nuget.org/packages/Burner-CLI"><img src="https://img.shields.io/nuget/v/Burner-CLI.svg" alt="NuGet" /></a>
<a href="https://www.nuget.org/packages/Burner-CLI"><img src="https://img.shields.io/nuget/dt/Burner-CLI.svg" alt="NuGet Downloads" /></a>
</p>

<p align="center">
<em>
A CLI tool to create and manage temporary dev projects for quick experiments.
</em>
</p>



## Requirements

### For Users
- [.NET 10.0](https://dotnet.microsoft.com/download) or later

### For Development
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [PowerShell](https://github.com/PowerShell/PowerShell) (for `dev.ps1` script)
- Git

### Dependencies
- [Spectre.Console](https://spectreconsole.net/) 0.49.1 – Rich console UI
- [Spectre.Console.Cli](https://spectreconsole.net/) 0.49.1 – Command-line parsing

### Test Framework
- [xUnit](https://xunit.net/) 2.9.3
- [coverlet](https://github.com/coverlet-coverage/coverlet) 6.0.4

## Installation

```
dotnet tool install -g Burner-CLI
```

## Features

- **Quick Scaffolding** – Spin up projects instantly using predefined templates
- **Organized Storage** – All projects live in a unified burner directory
- **Auto-Naming** – Projects are prefixed with `YYMMDD-NAME` format for easy identification
- **Easy Cleanup** – Remove projects when done, with auto-cleaning of old experiments
- **Extensible** – Add your own custom templates

## Commands

### `burner new <template> [name]`

Create a new project from a template. Name is auto-generated if not provided.

```bash
burner new dotnet                    # Auto-name: dotnet-HHMMSS
burner new dotnet my-experiment      # Create .NET console app
burner new web quick-test            # Create HTML/JS/CSS project
burner new react my-app -d ~/code    # Use custom directory
```

### `burner list`

List all burner projects.

```bash
burner list        # List projects
burner ls          # Alias
burner list -a     # Show full paths
```

### `burner burn`

Delete projects (cleanup).

```bash
burner burn my-experiment     # Delete specific project
burner burn --days 30         # Delete projects older than 30 days
burner burn -f --days 7       # Force delete without confirmation
burner burn -i                # Interactive mode: select from list
```

### `burner open [name]`

Open a project directory. Interactive mode if no name provided.

```bash
burner open                     # Interactive: select project and action
burner open -i                  # Interactive mode (explicit)
burner open my-experiment       # Print project path
burner open my-experiment -c    # Open in editor (uses configured editor)
burner open my-experiment -e    # Open in file explorer
```

### `burner config`

View and update configuration.

```bash
burner config                      # Show current config
burner config --home ~/projects    # Set burner home directory
burner config --templates ~/tpl    # Set templates directory
burner config --auto-clean-days 60 # Set cleanup threshold
burner config --editor cursor      # Set default editor (code, cursor, rider, etc.)
burner config --open-home          # Open burner home in explorer
burner config --open-templates     # Open templates dir in explorer
```

### `burner templates`

List available templates (built-in and custom).

```bash
burner templates
```

### `burner stats`

Show project statistics with charts.

```bash
burner stats    # Overview, projects by template, age distribution
```

## Built-in Templates

| Template | Description |
|----------|-------------|
| `dotnet` | .NET Console Application |
| `web`    | HTML + JS + CSS Web App |

## Custom Templates

Extend Burner with your own templates by adding executable scripts to the `templates` directory.

### Template Requirements

- Scripts can be written in any language (bash, PowerShell, Python, etc.)
- Must be executable from the command line
- Script filename becomes the template alias (e.g., `react.ps1` → `react`)

### Environment Variables

Burner sets these environment variables before running your template script:

| Variable | Description | Example |
|----------|-------------|---------|
| `BURNER_NAME` | User-provided project name | `my-experiment` |
| `BURNER_PATH` | Full path to project directory | `/home/user/.burner/projects/260107-my-experiment` |
| `BURNER_DATED_NAME` | Dated folder name | `260107-my-experiment` |

The working directory is automatically set to `BURNER_PATH`, so you can create files directly without changing directories.

### Example Templates

**PowerShell (`react.ps1`):**
```powershell
#!/usr/bin/env pwsh
# Creates a React app using Vite
npm create vite@latest . -- --template react
npm install
```

**Bash (`react.sh`):**
```bash
#!/bin/bash
# Creates a React app using Vite
set -e
npm create vite@latest . -- --template react
npm install
```

**Python (`python.py`):**
```python
#!/usr/bin/env python3
import os
name = os.environ['BURNER_NAME']
with open('main.py', 'w') as f:
    f.write(f'# {name}\nprint("Hello from {name}!")\n')
with open('requirements.txt', 'w') as f:
    f.write('')
```

## Configuration

Burner can be configured via a config file located at `~/.burner/config.json`:

```json
{
  "burnerHome": "~/.burner/projects",
  "burnerTemplates": "~/.burner/templates",
  "autoCleanDays": 30,
  "editor": "code"
}
```

| Option | Description | Default |
|--------|-------------|---------|
| `burnerHome` | Where new projects are created | `~/.burner/projects` |
| `burnerTemplates` | Where custom template scripts are stored | `~/.burner/templates` |
| `autoCleanDays` | Auto-remove projects older than this (0 to disable) | `30` |
| `editor` | Default editor command for opening projects | `code` |

## Development

For local development and testing, use the `dev.ps1` script:

```powershell
# Full test: uninstall, pack, install, and run test commands
./dev.ps1

# Just uninstall the tool
./dev.ps1 -Uninstall

# Run without reinstalling
./dev.ps1 -Run

# Skip install, just run verification
./dev.ps1 -SkipInstall
```


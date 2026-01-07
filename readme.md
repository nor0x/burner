# Burner 🔥


<img src="Art/icon_alpha.png" alt="Burner Logo" width="128" align="right" />



A CLI tool to create and manage temporary dev projects for quick experiments.

*Spark ideas. Burn when done.*

## Installation

```
dotnet tool install -g burner
```

## Features

- **Quick Scaffolding** – Spin up projects instantly using predefined templates
- **Organized Storage** – All projects live in a unified burner directory
- **Auto-Naming** – Projects are prefixed with `YYMMDD-NAME` format for easy identification
- **Easy Cleanup** – Remove projects when done, with auto-cleaning of old experiments
- **Extensible** – Add your own custom templates

## Commands

### `burner new <template> <name>`

Create a new project from a template.

```bash
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

### `burner open <name>`

Open a project directory.

```bash
burner open my-experiment       # Print project path
burner open my-experiment -c    # Open in VS Code
burner open my-experiment -e    # Open in file explorer
```

### `burner config`

View and update configuration.

```bash
burner config                      # Show current config
burner config --home ~/projects    # Set burner home directory
burner config --templates ~/tpl    # Set templates directory
burner config --auto-clean-days 60 # Set cleanup threshold
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

### Requirements

- Scripts can be written in any language (bash, PowerShell, Python, etc.)
- Must be executable from the command line
- Script filename becomes the template alias

### Script Arguments

| Argument | Description |
|----------|-------------|
| `$1`     | Project name |
| `$2`     | Target directory (uses configured default if not provided) |

### Example

A template script named `react.ps1` in the templates directory would be invoked as:

```
burner new react my-experiment
```

## Configuration

Burner can be configured via a config file located at `~/.burner/config.json`:

```json
{
  "burnerHome": "~/.burner/projects",
  "burnerTemplates": "~/.burner/templates",
  "autoCleanDays": 30
}
```

| Option | Description | Default |
|--------|-------------|---------|
| `burnerHome` | Where new projects are created | `~/.burner/projects` |
| `burnerTemplates` | Where custom template scripts are stored | `~/.burner/templates` |
| `autoCleanDays` | Auto-remove projects older than this (0 to disable) | `30` |


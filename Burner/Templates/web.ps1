#!/usr/bin/env pwsh
# Burner built-in template: HTML + JS + CSS Web App
# Environment: BURNER_NAME, BURNER_PATH, BURNER_DATED_NAME
# Working directory is already set to project path

@"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>$env:BURNER_NAME</title>
    <link rel="stylesheet" href="styles.css">
</head>
<body>
    <h1>$env:BURNER_NAME</h1>
    <p>Your web experiment starts here!</p>
    <script src="script.js"></script>
</body>
</html>
"@ | Set-Content "index.html"

@"
* { margin: 0; padding: 0; box-sizing: border-box; }
body {
    font-family: system-ui, -apple-system, sans-serif;
    line-height: 1.6;
    padding: 2rem;
    max-width: 800px;
    margin: 0 auto;
    background: #1a1a2e;
    color: #eee;
}
h1 { color: #ff6b35; margin-bottom: 1rem; }
p { color: #aaa; }
"@ | Set-Content "styles.css"

@"
console.log('$env:BURNER_NAME loaded!');
document.addEventListener('DOMContentLoaded', () => console.log('DOM ready'));
"@ | Set-Content "script.js"

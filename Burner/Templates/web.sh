#!/bin/bash
# Burner built-in template: HTML + JS + CSS Web App
# Environment: BURNER_NAME, BURNER_PATH, BURNER_DATED_NAME
# Working directory is already set to project path
set -e

cat > index.html << EOF
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>$BURNER_NAME</title>
    <link rel="stylesheet" href="styles.css">
</head>
<body>
    <h1>$BURNER_NAME</h1>
    <p>Your web experiment starts here!</p>
    <script src="script.js"></script>
</body>
</html>
EOF

cat > styles.css << 'EOF'
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
EOF

cat > script.js << EOF
console.log('$BURNER_NAME loaded!');
document.addEventListener('DOMContentLoaded', () => console.log('DOM ready'));
EOF

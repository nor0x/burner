#!/bin/bash
# Burner built-in template: Python Hello World
# Environment: BURNER_NAME, BURNER_PATH, BURNER_DATED_NAME
# Working directory is already set to project path
set -e

cat > README.md << EOF
# Python Burner Project: $BURNER_NAME

A tiny Python starter created with Burner.
EOF

cat > main.py << 'EOF'
print("Hello, world!")
EOF

cat > .gitignore << 'EOF'
.venv/
__pycache__/
*.pyc
EOF

touch requirements.txt

#!/bin/bash
# Burner built-in template: Empty Git repository with README
# Environment: BURNER_NAME, BURNER_PATH, BURNER_DATED_NAME
# Working directory is already set to project path
set -e

if ! command -v git >/dev/null 2>&1; then
  echo "git is required for the 'git' template" >&2
  exit 1
fi

git init >/dev/null

cat > README.md << EOF
# $BURNER_NAME
EOF

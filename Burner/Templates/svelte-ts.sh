#!/bin/bash
# BURNER_INTERACTIVE
# svelte-ts.sh - Creates a Svelte TypeScript app using Vite

set -e
cd "$BURNER_PATH"
npm create vite@latest . -- --template svelte-ts
npm install
npm pkg set name="$BURNER_NAME"

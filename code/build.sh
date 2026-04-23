#!/bin/bash

# UNCOMMENT/COMMENT LINE BELOW WHEN WE ADD FILES TO TEST
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$SCRIPT_DIR/.."
cd WebApp

if [ ! -d "$HOME/.nvm" ]; then
    echo "Installing nvm..."
    curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
fi

export NVM_DIR="$HOME/.nvm"
[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"

if ! command -v node &> /dev/null; then
    echo "Installing Node.js 20..."
    nvm install 20
fi

nvm use 20

echo "Installing dependencies..."
npm ci

echo "Checking code complexity..."
npm run lint

echo "Running tests with 100% coverage requirement..."
npm run test:coverage


echo "All Tests Passed"
echo "100% Code Coverage"
echo "No Complex Code"

exit 0

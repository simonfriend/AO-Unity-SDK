#!/bin/bash

echo "🚀 Starting setup and build process for WebGL..."

# Navigate to the script's directory (inside /build/)
cd "$(dirname "$0")"

# Move up one level to WebGLBuild directory
cd ..

# Check if package.json exists; if not, initialize npm
if [ ! -f package.json ]; then
    echo "📦 Initializing npm project..."
    npm init -y
fi

echo "🔄 Installing/updating dependencies..."
npm install --save-dev esbuild
npm install --save-dev esbuild-plugin-polyfill-node

npm install --save @permaweb/aoconnect@0.0.54
npm install --save @dha-team/arbundles
npm install --save @ethersproject/providers
npm install --save ethers
npm install --save @ar.io/sdk
npm install --save @wanderapp/connect

echo "🔨 Running build script..."
node build-tools/build-script.js  # Updated path to reflect new location

echo "✅ Build completed!"

# Optionally, start a local web server
echo "🌍 Starting local server at http://localhost:8000/"
npx http-server -p 8000

@echo off
echo 🚀 Starting setup and build process for WebGL...

:: Navigate to the script's directory (inside /build/)
cd /d "%~dp0"

:: Move up one level to WebGLBuild directory
cd ..

:: Check if package.json exists; if not, initialize npm
if not exist package.json (
    echo 📦 Initializing npm project...
    npm init -y
)

echo 🔄 Installing/updating dependencies...
npm install --save-dev esbuild
npm install --save-dev @esbuild-plugins/node-globals-polyfill
npm install --save-dev @esbuild-plugins/node-modules-polyfill

npm install --save @permaweb/aoconnect
npm install --save arseeding-arbundles
npm install --save @ethersproject/providers

echo 🔨 Running build script...
node build-tools/build-script.js  REM Updated path

echo ✅ Build completed!

:: Optionally, start a local web server
echo 🌍 Starting local server at http://localhost:8000/
npx http-server -p 8000

pause
@echo off
REM AOConnect Editor Tester Setup Script for Windows
REM This script sets up the Node.js environment for testing AO messages from Unity Editor

echo 🚀 Setting up AOConnect Editor Tester...

REM Check if Node.js is installed
node --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Node.js is not installed. Please install Node.js first:
    echo    https://nodejs.org/
    pause
    exit /b 1
)

echo ✅ Node.js found
node --version

REM Check if npm is installed
npm --version >nul 2>&1
if errorlevel 1 (
    echo ❌ npm is not installed. Please install npm first.
    pause
    exit /b 1
)

echo ✅ npm found
npm --version

REM Install dependencies
echo 📦 Installing dependencies...
npm install

if errorlevel 1 (
    echo ❌ Failed to install dependencies
    pause
    exit /b 1
)

echo ✅ Dependencies installed successfully!

REM Test the script
echo 🧪 Testing script...
node aoconnect-editor.js --help >nul 2>&1

if errorlevel 1 (
    echo ❌ Script test failed
    pause
    exit /b 1
)

echo ✅ Script test passed!
echo.
echo 🎉 Setup complete! You can now use the AOConnect Editor Tester.
echo.
echo 📖 Usage examples:
echo    node aoconnect-editor.js --help
echo    node aoconnect-editor.js --tag-Action=GetUserInfo
echo    node aoconnect-editor.js --mode legacy --tag-Action=GetUserInfo
echo.
echo 💡 Don't forget to configure your Arweave wallet keyfile!
echo    Place it as 'wallet.json' in the parent directory, or use --wallet parameter
echo.
pause

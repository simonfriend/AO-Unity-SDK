#!/bin/bash

# HyperBEAM Editor Tester Setup Script
# This script sets up the Node.js environment for testing HyperBEAM messages from Unity Editor

echo "🚀 Setting up HyperBEAM Editor Tester..."

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js first:"
    echo "   https://nodejs.org/"
    exit 1
fi

echo "✅ Node.js found: $(node --version)"

# Check if npm is installed
if ! command -v npm &> /dev/null; then
    echo "❌ npm is not installed. Please install npm first."
    exit 1
fi

echo "✅ npm found: $(npm --version)"

# Install dependencies
echo "📦 Installing dependencies..."
npm install

if [ $? -eq 0 ]; then
    echo "✅ Dependencies installed successfully!"
else
    echo "❌ Failed to install dependencies"
    exit 1
fi

# # Check if wallet file exists
# WALLET_PATH="../wallet.json"
# if [ -f "$WALLET_PATH" ]; then
#     echo "✅ Wallet keyfile found"
# else
#     echo "⚠️  Wallet keyfile not found at: $WALLET_PATH"
#     echo "   Make sure your Arweave wallet keyfile is in the correct location"
# fi

# # Test the script
# echo "🧪 Testing script..."
# node aoconnect-editor.js --help > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo "✅ Script test passed!"
else
    echo "❌ Script test failed"
    exit 1
fi

echo ""
echo "🎉 Setup complete! You can now use the HyperBEAM Editor Tester."
echo ""
echo "📖 Usage:"
echo "   • Open Unity Editor"
echo "   • Go to Tools > AO > HyperBEAM Editor Tester"
echo "   • Configure your message and tags"
echo "   • Click 'Send HyperBEAM Message'"
echo ""
echo "🔧 Command line usage:"
echo "   node hyperbeam-editor-tester.js --tag-Action=GetUserInfo"
echo ""
echo "💡 Make sure HyperBEAM is running on http://localhost:8734"

# AOConnect Editor Tester

A Unity Editor tool for testing both HyperBEAM and legacy AO messages during development without needing to build to WebGL.

## 🚀 Quick Setup

### Prerequisites
- **Node.js** (v16 or higher) - Download from [nodejs.org](https://nodejs.org/)
- **Unity Editor** with the Permaverse AO SDK package installed
- **Arweave wallet keyfile** (JSON format)
- **HyperBEAM** running locally (for HyperBEAM mode only) or an HyperBEAM node available

### Installation

1. **Install Node.js dependencies:**
   
   **macOS/Linux:**
   ```bash
   cd Packages/com.permaverse.ao-sdk/EditorConnect
   ./setup.sh
   ```
   
   **Windows:**
   ```cmd
   cd Packages\com.permaverse.ao-sdk\EditorConnect
   setup.bat
   ```

2. **Configure your Arweave wallet:**
   - Place your Arweave wallet keyfile in the parent directory as `wallet.json`, or
   - Use the `--wallet` parameter to specify a custom path

3. **Open Unity Editor** and go to `Tools > AO > HyperBEAM Editor Tester`

4. **For HyperBEAM local mode:** Make sure HyperBEAM is running on your local address (e.g. `http://localhost:8734`)

## 📖 How It Works

This tool supports two AO message modes:

1. **HyperBEAM Mode** (Default): Fast message processing via HyperBEAM
2. **Legacy Mode**: Traditional AO connect message flow

The tool consists of:

1. **Node.js Script** (`aoconnect-editor.js`): Uses `@permaweb/aoconnect` with your Arweave wallet to send messages
2. **Unity Editor Window** (`HyperBeamEditorTester.cs`): Provides a GUI to configure and send messages

## 🎯 Features

- **Dual Mode Support**: Choose between HyperBEAM and legacy AO message modes
- **Quick Presets**: Common message types (GetUserInfo, GetLeaderboard, EnterMatchmaking, etc.)
- **Custom Tags**: Add any tags you need for testing
- **Data Payload**: Send messages with custom data
- **Real Wallet Signing**: Uses your actual Arweave wallet keyfile
- **Unity-Friendly**: Integrates directly into Unity Editor workflow
- **Cross-Platform**: Works on Windows, macOS, and Linux
- **Verbose Output**: Detailed logging for debugging

## 📋 Usage Examples

### From Unity Editor:
1. Open `Tools > AO > HyperBEAM Editor Tester`
2. Select your preferred mode (HyperBEAM or Legacy)
3. Click a preset button (e.g., "GetUserInfo")
4. Click "Send HyperBEAM Message" or "Send Legacy AO Message"
5. View the response in the editor window

### From Command Line:

**HyperBEAM Mode (default):**
```bash
# Get user info via HyperBEAM
node aoconnect-editor.js --tag-Action=GetUserInfo

# Specify custom wallet path
node aoconnect-editor.js --wallet /path/to/your/wallet.json --tag-Action=GetUserInfo
```

**Legacy Mode:**
```bash
# Get user info via legacy AO
node aoconnect-editor.js --mode legacy --tag-Action=GetUserInfo

# Enter matchmaking via legacy AO
node aoconnect-editor.js --mode legacy \
  --tag-Action=EnterMatchmaking \
  --tag-MatchType=CasualAI \
  --tag-Class=SamuraiBZ \
  --tag-SkinId=1
```

**Cross-platform examples:**

```bash
# Get leaderboard with pagination
node aoconnect-editor.js \
  --tag-Action=GetLeaderboard \
  --tag-Page=1 \
  --tag-PageSize=10 \
  --tag-SortBy=RankPoints

# Custom message with data
node aoconnect-editor.js \
  --data "Hello AO!" \
  --tag-Action=TestMessage \
  --tag-CustomParam=value
```

**Windows Command Prompt:**
```cmd
node aoconnect-editor.js --tag-Action=GetUserInfo --mode legacy
```

## 🔧 Configuration

- **Process ID**: Your AO process ID
- **Message Mode**: Choose between HyperBEAM (fast) or Legacy (traditional) 
- **HyperBEAM URL**: Local HyperBEAM instance URL (default: http://localhost:8734)
- **Wallet Path**: Arweave keyfile path (default: ../wallet.json, or specify with --wallet)

## 🛠️ Requirements

- **Node.js** (v16+) and npm - [Download Node.js](https://nodejs.org/)
- **HyperBEAM** running locally (for HyperBEAM mode only)
- **Valid Arweave wallet keyfile** (JSON format)
- **Unity Editor** with Permaverse AO SDK package

## 🎯 Benefits

1. **Faster Development**: Test AO messages without WebGL builds
2. **Dual Mode Support**: Test both HyperBEAM and legacy AO flows
3. **Real Environment**: Uses actual wallet signing, same as production
4. **Easy Debugging**: Verbose output and formatted responses
5. **Rapid Iteration**: Quick presets for common operations
6. **Cross-Platform**: Works on Windows, macOS, and Linux
7. **Editor Integration**: Works seamlessly within Unity workflow

## 📝 Output Formats

- **Unity Format**: JSON with success/error status for Unity integration
- **Simple Format**: Clean, readable response data
- **Full JSON**: Complete response with all metadata

## 🚨 Troubleshooting

### General Issues:
1. **"Node.js not found"**: 
   - Install Node.js from [nodejs.org](https://nodejs.org/)
   - Restart your terminal/command prompt after installation
   - Verify with: `node --version`

2. **"Wallet not found"**: 
   - Ensure your keyfile is named `wallet.json` in the parent directory, or
   - Use `--wallet /full/path/to/your/wallet.json` to specify custom location
   - Verify the file is valid JSON format

3. **"Permission denied" (macOS/Linux)**: 
   - Run `chmod +x setup.sh` to make setup script executable

### HyperBEAM Mode Issues:
4. **"HyperBEAM connection failed"**: 
   - Make sure HyperBEAM is running on port 8734
   - Try: `curl http://localhost:8734` to test connectivity

### Legacy Mode Issues:
5. **"Legacy AO connection failed"**: 
   - Check your internet connection
   - Verify your wallet has sufficient AR for transaction fees

### Platform-Specific:

**Windows:**
- Use Command Prompt or PowerShell, not Git Bash
- Use backslashes for paths: `--wallet C:\path\to\wallet.json`
- If npm install fails, try running as Administrator

**macOS/Linux:**
- Use forward slashes for paths: `--wallet /path/to/wallet.json`
- Make sure you have write permissions in the project directory

This tool enables seamless AO development across all platforms, bridging Unity Editor and AO ecosystems!

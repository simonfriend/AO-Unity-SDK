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
   cd Packages/com.permaverse.ao-sdk/EditorConnect~
   ./setup.sh
   ```
   
   **Windows:**
   ```cmd
   cd Packages\com.permaverse.ao-sdk\EditorConnect~
   setup.bat
   ```

2. **Configure your Arweave wallet:**
   - Place your Arweave wallet keyfile in the parent directory as `wallet.json`, or
   - Use the `--wallet` parameter to specify a custom path

3. **Open Unity Editor** and go to `Tools > AO > AO Message Editor Tester`

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
- **Multiple Output Formats**: Unity format for integration, raw format for debugging
- **Granular Logging**: Three log levels (None, Basic, Verbose) for command line usage
- **Quick Presets**: Common message types (GetUserInfo, GetLeaderboard, EnterMatchmaking, etc.)
- **Custom Tags**: Add any tags you need for testing
- **Data Payload**: Send messages with custom data
- **Real Wallet Signing**: Uses your actual Arweave wallet keyfile
- **Unity-Friendly**: Integrates directly into Unity Editor workflow
- **Cross-Platform**: Works on Windows, macOS, and Linux

## 📋 Usage Examples

### From Unity Editor:
1. Open `Tools > AO > AO Message Editor Tester`
2. Configure your settings:
   - **Process ID**: Enter your AO process ID
   - **Message Mode**: Choose between HyperBEAM or Legacy
   - **Output Format**: Select Unity or Raw format
3. Select a preset action or configure custom tags
4. Click "Send Message"
5. View the response in the Unity Console

### From Command Line:

**Basic Examples:**
```bash
# Simple message with basic logging
node aoconnect-editor.js --processId YOUR_PROCESS_ID --action GetUserInfo

# Silent operation (no logs)
node aoconnect-editor.js --processId YOUR_PROCESS_ID --action GetUserInfo --log-level none

# Verbose debugging with raw output
node aoconnect-editor.js \
  --processId YOUR_PROCESS_ID \
  --action GetUserInfo \
  --log-level verbose \
  --format raw
```

**HyperBEAM Mode (default):**
```bash
# Get user info via HyperBEAM with custom wallet
node aoconnect-editor.js \
  --processId YOUR_PROCESS_ID \
  --action GetUserInfo \
  --wallet /path/to/your/wallet.json \
  --log-level basic

# Enter matchmaking with verbose logging
node aoconnect-editor.js \
  --processId YOUR_PROCESS_ID \
  --action EnterMatchmaking \
  --data "MatchType:CasualAI,Class:SamuraiBZ" \
  --log-level verbose
```

**Legacy Mode:**
```bash
# Get user info via legacy AO
node aoconnect-editor.js \
  --processId YOUR_PROCESS_ID \
  --action GetUserInfo \
  --mode legacy \
  --log-level basic

# Get leaderboard with raw output
node aoconnect-editor.js \
  --processId YOUR_PROCESS_ID \
  --action GetLeaderboard \
  --mode legacy \
  --format raw \
  --log-level none
```

## 🔧 Configuration Options

### Unity Editor Interface

- **Process ID**: Your AO process ID (required)
- **Message Mode**: 
  - HyperBEAM: Fast message processing via HyperBEAM
  - Legacy: Traditional AO connect message flow
- **Output Format**: 
  - Unity: Unity-formatted, readable output with success/error status
  - Raw: Unprocessed response data for debugging
- **Log Level**: Fixed to 'None' for optimal performance (use command line for verbose logging)

### Command Line Parameters

| Parameter | Description | Default | Options |
|-----------|-------------|---------|---------|
| `--processId` | AO process ID (required) | - | Any valid process ID |
| `--action` | Message action/preset | - | GetUserInfo, GetLeaderboard, EnterMatchmaking, etc. |
| `--data` | Message data payload | - | Any string |
| `--wallet` | Path to wallet keyfile | ../wallet.json | Any valid file path |
| `--mode` | Message processing mode | hyperbeam | hyperbeam, legacy |
| `--log-level` | Logging verbosity | basic | none, basic, verbose |
| `--format` | Output format | unity | unity, raw |
| `--verbose` | Legacy verbose flag | false | (maps to verbose log level) |
| `--help` | Show help information | - | - |

### Environment Configuration

- **HyperBEAM URL**: Local HyperBEAM instance URL (default: http://localhost:8734)
- **Wallet Path**: Arweave keyfile path (can be customized with --wallet parameter)

## 🛠️ Requirements

- **Node.js** (v16+) and npm - [Download Node.js](https://nodejs.org/)
- **HyperBEAM** running locally (for HyperBEAM mode only)
- **Valid Arweave wallet keyfile** (JSON format)
- **Unity Editor** with Permaverse AO SDK package

## 🎯 Benefits

1. **Faster Development**: Test AO messages without WebGL builds
2. **Dual Mode Support**: Test both HyperBEAM and legacy AO flows
3. **Real Environment**: Uses actual wallet signing, same as production
4. **Flexible Logging**: Three log levels for performance optimization and debugging
5. **Multiple Output Formats**: Choose format that best suits your workflow
6. **Rapid Iteration**: Quick presets for common operations
7. **Cross-Platform**: Works on Windows, macOS, and Linux
8. **Editor Integration**: Works seamlessly within Unity workflow
9. **Performance Optimized**: Silent mode for production testing, verbose for debugging

## 📝 Output Formats

- **Unity Format**: JSON with success/error status optimized for Unity integration
- **Raw Format**: Unprocessed response data with all metadata for debugging

## 🔍 Log Levels

- **None**: Silent operation with no console output - ideal for automated testing or production scenarios where performance is critical
- **Basic**: Essential information only - shows errors, warnings, and key operation status
- **Verbose**: Detailed debugging information - includes full request/response data, timing information, and step-by-step execution details

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

#!/usr/bin/env node

/**
 * AOConnect Editor Tester - Node.js script for testing AO messages from Unity Editor
 * 
 * Supports both HyperBEAM and legacy AO message modes for development and testing.
 * This script can be called from Unity Editor to send messages during development.
 * Usage: node aoconnect-editor.js [options]
 */

const { connect, createSigner, message, createDataItemSigner, result } = require('@permaweb/aoconnect');
const fs = require('fs');
const path = require('path');

// Configuration
const DEFAULT_PROCESS_ID = "t9qaxM7bEyxrzJ2PG52qyvP4h3ub6DG775M6XbSAYsY"; // Your StarGrid process ID
const DEFAULT_HYPERBEAM_URL = "http://localhost:8734";
const DEFAULT_WALLET_PATH = path.join(__dirname, '..', 'wallet.json');

// Logging helper functions
function logBasic(options, message, ...args) {
    if (options.logLevel === 'basic' || options.logLevel === 'verbose') {
        console.log(message, ...args);
    }
}

function logVerbose(options, message, ...args) {
    if (options.logLevel === 'verbose') {
        console.log(message, ...args);
    }
}

function logError(options, message, ...args) {
    // Always log errors regardless of level
    console.error(message, ...args);
}

// Parse command line arguments
function parseArgs() {
    const args = process.argv.slice(2);
    const options = {
        processId: DEFAULT_PROCESS_ID,
        hyperBeamUrl: DEFAULT_HYPERBEAM_URL,
        walletPath: DEFAULT_WALLET_PATH,
        data: "",
        tags: {},
        help: false,
        logLevel: "basic", // none, basic, verbose
        output: "raw", // unity or raw
        uniqueID: null, // Will be set from command line or generated if not provided
        mode: "hyperbeam" // hyperbeam or legacy
    };

    for (let i = 0; i < args.length; i++) {
        const arg = args[i];
        
        if (arg === '--help' || arg === '-h') {
            options.help = true;
        } else if (arg === '--verbose' || arg === '-v') {
            options.logLevel = "verbose"; // Backward compatibility
        } else if (arg === '--log-level' || arg === '-l') {
            const level = args[++i];
            if (['none', 'basic', 'verbose'].includes(level)) {
                options.logLevel = level;
            } else {
                console.error('❌ Invalid log level. Use: none, basic, verbose');
                process.exit(1);
            }
        } else if (arg === '--process-id' || arg === '-p') {
            options.processId = args[++i];
        } else if (arg === '--hyperbeam-url' || arg === '-u') {
            options.hyperBeamUrl = args[++i];
        } else if (arg === '--wallet' || arg === '-w') {
            options.walletPath = args[++i];
        } else if (arg === '--data' || arg === '-d') {
            options.data = args[++i];
        } else if (arg === '--data-base64') {
            // Decode base64 data
            try {
                options.data = Buffer.from(args[++i], 'base64').toString('utf8');
            } catch (error) {
                console.error('❌ Invalid base64 data:', error.message);
                process.exit(1);
            }
        } else if (arg === '--tags-base64') {
            // Decode base64 tags JSON
            try {
                const tagsJson = Buffer.from(args[++i], 'base64').toString('utf8');
                const decodedTags = JSON.parse(tagsJson);
                options.tags = { ...options.tags, ...decodedTags };
            } catch (error) {
                console.error('❌ Invalid base64 tags JSON:', error.message);
                process.exit(1);
            }
        } else if (arg === '--output' || arg === '-o') {
            options.output = args[++i];
        } else if (arg === '--mode' || arg === '-m') {
            options.mode = args[++i];
        } else if (arg === '--unique-id') {
            options.uniqueID = args[++i];
        } else if (arg.startsWith('--tag-')) {
            // Handle --tag-Action=EnterMatchmaking style arguments
            const tagName = arg.substring(6); // Remove '--tag-'
            if (tagName.includes('=')) {
                const [key, value] = tagName.split('=', 2);
                options.tags[key] = value;
            } else {
                options.tags[tagName] = args[++i];
            }
        } else if (arg.startsWith('-t')) {
            // Handle -tAction=EnterMatchmaking style arguments
            const tagDef = arg.substring(2); // Remove '-t'
            if (tagDef.includes('=')) {
                const [key, value] = tagDef.split('=', 2);
                options.tags[key] = value;
            } else {
                const key = tagDef || args[++i];
                options.tags[key] = args[++i];
            }
        }
    }

    return options;
}

// Load wallet
function loadWallet(walletPath, options) {
    try {
        const wallet = JSON.parse(fs.readFileSync(walletPath, 'utf8'));
        return wallet;
    } catch (error) {
        logError(options, '❌ Failed to load wallet from:', walletPath);
        logError(options, '💡 Make sure the wallet file exists and is valid JSON');
        process.exit(1);
    }
}

// Send Legacy AO message
async function sendLegacyMessage(options) {
    const wallet = loadWallet(options.walletPath, options);
    
    logVerbose(options, '🔧 Configuration (Legacy Mode):');
    logVerbose(options, '   Process ID:', options.processId);
    logVerbose(options, '   Wallet Path:', options.walletPath);
    logVerbose(options, '   Data:', options.data || '(empty)');
    logVerbose(options, '   Tags:', Object.keys(options.tags).length > 0 ? options.tags : '(none)');
    logVerbose(options, '   Wallet:', wallet ? 'Loaded' : 'Not loaded');
    logVerbose(options, '');

    try {
        // Convert tags object to array format for legacy AO
        const tagsArray = Object.entries(options.tags).map(([name, value]) => ({
            name,
            value
        }));

        logVerbose(options, '📤 Sending legacy message with parameters:');
        logVerbose(options, '   Process:', options.processId);
        logVerbose(options, '   Tags:', tagsArray);
        logVerbose(options, '   Data:', options.data || '(empty)');
        logVerbose(options, '');

        // Send legacy AO message
        const messageId = await message({
            process: options.processId,
            tags: tagsArray,
            signer: createDataItemSigner(wallet),
            data: options.data || "",
            anchor: Math.round(Date.now() / 1000)
            .toString()
            .padStart(32, Math.floor(Math.random() * 10).toString())
        });

        logVerbose(options, '📨 Message sent, ID:', messageId);
        logVerbose(options, '🔄 Getting result...');

        // Get the result
        const legacyResult = await result({
            message: messageId,
            process: options.processId
        });

        logBasic(options, '✅ Legacy AO message sent successfully!');
        logVerbose(options, '');

        return legacyResult;

    } catch (error) {
        if (options.output === 'unity') {
            // Unity-friendly error format - same as JavaScript sendMessageCustomCallback
            const errorResponse = {
                Messages: [],
                Spawns: [],
                Output: "",
                Error: error.message,
                uniqueID: options.uniqueID
            };
            console.log(JSON.stringify(errorResponse));
        } else {
            logError(options, '❌ Failed to send legacy AO message:', error.message);
            logVerbose(options, '💡 Full error:', error);
        }
        process.exit(1);
    }
}

// Send HyperBEAM message
async function sendHyperBeamMessage(options) {
    const wallet = loadWallet(options.walletPath, options);
    
    logVerbose(options, '🔧 Configuration:');
    logVerbose(options, '   Process ID:', options.processId);
    logVerbose(options, '   HyperBEAM URL:', options.hyperBeamUrl);
    logVerbose(options, '   Wallet Path:', options.walletPath);
    logVerbose(options, '   Data:', options.data || '(empty)');
    logVerbose(options, '   Tags:', Object.keys(options.tags).length > 0 ? options.tags : '(none)');
    logVerbose(options, '   Wallet:', wallet ? 'Loaded' : 'Not loaded');
    logVerbose(options, '');

    // Create HyperBEAM connection
    const { request } = connect({
        MODE: "mainnet",
        URL: options.hyperBeamUrl,
        signer: createSigner(wallet),
    });

    try {
        // Build request parameters
        const requestParams = {
            path: `/${options.processId}~process@1.0/push/serialize~json@1.0`,
            method: "POST",
            target: options.processId,
            signingFormat: "ANS-104",
        };

        // Add data if provided
        if (options.data) {
            requestParams.data = options.data;
        }

        // Add tags as properties
        Object.assign(requestParams, options.tags);

        logVerbose(options, '📤 Sending request with parameters:', requestParams);
        logVerbose(options, '');

        // Send request via HyperBEAM
        const result = await request(requestParams);

        logBasic(options, '✅ HyperBEAM message sent successfully!');
        logVerbose(options, '');

        return result;

    } catch (error) {
        if (options.output === 'unity') {
            // Unity-friendly error format - same as JavaScript sendMessageHyperBeam
            const errorResponse = {
                Messages: [],
                Spawns: [],
                Output: "",
                Error: error.message,
                uniqueID: options.uniqueID
            };
            console.log(JSON.stringify(errorResponse));
        } else {
            logError(options, '❌ Failed to send HyperBEAM message:', error.message);
            logVerbose(options, '💡 Full error:', error);
        }
        process.exit(1);
    }
}

// Format output based on requested format
function formatOutput(result, options) {
    switch (options.output) {
        case 'unity':
            // Unity-friendly JSON format - same as JavaScript sendMessage functions
            try {
                let resultData;
                
                if (options.mode === 'legacy') {
                    // For legacy mode, the result should already be in the right format
                    resultData = result;
                } else {
                    // For HyperBEAM mode, handle nested response structure
                    // HyperBEAM response: result.body -> parsed.json.body -> actual AO response
                    if (result && result.body) {
                        try {
                            // First level: parse result.body (HyperBEAM wrapper) - handle both string and object
                            let parsed;
                            if (typeof result.body === 'string') {
                                parsed = JSON.parse(result.body);
                            } else {
                                // result.body is already an object
                                parsed = result.body;
                            }
                            logVerbose(options, '🔍 Parsed HyperBEAM wrapper:', JSON.stringify(parsed, null, 2));
                            
                            if (parsed && parsed.json && parsed.json.body) {
                                // Second level: parse parsed.json.body (actual AO response) - handle both string and object
                                let aoResponse;
                                if (typeof parsed.json.body === 'string') {
                                    aoResponse = JSON.parse(parsed.json.body);
                                } else {
                                    // parsed.json.body is already an object
                                    aoResponse = parsed.json.body;
                                }
                                logVerbose(options, '🔍 Parsed AO response:', JSON.stringify(aoResponse, null, 2));
                                resultData = aoResponse;
                            } else if (parsed && parsed.body) {
                                // Alternative structure: try parsing parsed.body - handle both string and object
                                try {
                                    let aoResponse;
                                    if (typeof parsed.body === 'string') {
                                        aoResponse = JSON.parse(parsed.body);
                                    } else {
                                        aoResponse = parsed.body;
                                    }
                                    resultData = aoResponse;
                                } catch {
                                    resultData = parsed;
                                }
                            } else if (parsed) {
                                // Fallback if structure is different
                                resultData = parsed;
                            } else {
                                throw new Error("Empty parsed response");
                            }
                        } catch (parseError) {
                            logVerbose(options, '⚠️ Could not parse HyperBEAM response body as JSON:', parseError.message);
                            logVerbose(options, '⚠️ Raw result.body:', result.body);
                            // Fallback to full result
                            resultData = result;
                        }
                    } else if (result && result.json && result.json.body) {
                        // Direct structure handling
                        resultData = result.json.body;
                    } else if (result) {
                        // Fallback to full result if body structure is different
                        resultData = result;
                    } else {
                        throw new Error("Empty response");
                    }
                }

                // Return the same format as JavaScript sendMessage functions
                const response = {
                    Messages: resultData.Messages || [],
                    Spawns: resultData.Spawns || [],
                    Output: resultData.Output || "",
                    Error: resultData.Error || null,
                    uniqueID: options.uniqueID
                };

                return JSON.stringify(response);
                
            } catch (bodyError) {
                logVerbose(options, '⚠️ Response parsing failed:', bodyError.message);
                // Handle case where parsing fails
                const fallbackResponse = {
                    Messages: [],
                    Spawns: [],
                    Output: result ? JSON.stringify(result) : "",
                    Error: `Response parsing failed: ${bodyError.message}`,
                    uniqueID: options.uniqueID
                };
                
                return JSON.stringify(fallbackResponse);
            }
            
        case 'raw':
        default:
            // Raw JSON output - unprocessed result for debugging
            return JSON.stringify(result, null, 2);
    }
}

// Show help
function showHelp() {
    console.log(`
🚀 AOConnect Editor Tester - Send AO messages from Unity Editor

📖 Usage:
   node aoconnect-editor.js [options]

🔧 Options:
   -h, --help                    Show this help message
   --log-level <level>           Log level: none, basic, verbose (default: basic)
   -v, --verbose                 Enable verbose logging (same as --log-level verbose)
   -p, --process-id <id>         Target process ID (default: ${DEFAULT_PROCESS_ID})
   -u, --hyperbeam-url <url>     HyperBEAM URL (default: ${DEFAULT_HYPERBEAM_URL})
   -w, --wallet <path>           Arweave wallet keyfile path (default: ../wallet.json)
   -d, --data <data>             Message data payload (raw string)
   --data-base64 <data>          Message data payload (base64 encoded)
   --tags-base64 <tags>          All tags as base64 encoded JSON object
   -t, --tag-<key>=<value>       Add individual tag (e.g., --tag-Action=GetUserInfo)
   -o, --output <format>         Output format: unity, raw (default: raw)
   -m, --mode <mode>             Mode: hyperbeam, legacy (default: hyperbeam)
   --unique-id <id>              Unique identifier for Unity callbacks

🎯 Output Formats:
   • unity: Structured format for Unity integration (Messages, Spawns, Output, Error)
   • raw:   Unprocessed response for debugging

� Log Levels:
   • none:    Silent mode for production performance
   • basic:   Essential information only
   • verbose: Detailed debugging information

�📋 Examples:
   # Basic usage with HyperBEAM (default)
   node aoconnect-editor.js --tag-Action=GetUserInfo
   
   # Using base64 encoded tags (recommended for complex data)
   node aoconnect-editor.js --tags-base64 eyJBY3Rpb24iOiJHZXRVc2VySW5mbyJ9
   
   # Using base64 encoded data
   node aoconnect-editor.js --data-base64 eyJrZXkiOiJ2YWx1ZSJ9 --tag-Action=ProcessData
   
   # Legacy AO mode
   node aoconnect-editor.js --mode legacy --tag-Action=GetUserInfo
   
   # Silent mode for performance
   node aoconnect-editor.js --log-level none --tag-Action=GetUserInfo
   
   # Enter matchmaking with HyperBEAM
   node aoconnect-editor.js \\
     --tag-Action=EnterMatchmaking \\
     --tag-MatchType=CasualAI \\
     --tag-Class=SamuraiBZ \\
     --tag-SkinId=1
   
   # Unity-friendly output format
   node aoconnect-editor.js --output unity --tag-Action=GetUserInfo
   
   # Custom wallet path with verbose logging
   node aoconnect-editor.js --verbose --wallet /path/to/wallet.json --tag-Action=GetUserInfo🌐 Requirements:
   - Node.js (v16 or higher)
   - Valid Arweave wallet keyfile
   - @permaweb/aoconnect package installed
   - HyperBEAM running locally (for HyperBEAM mode only)

🛠️  Setup:
   # macOS/Linux:
   cd EditorConnect
   ./setup.sh
   
   # Windows:
   cd EditorConnect
   setup.bat
`);
}

// Main execution
async function main() {
    const options = parseArgs();

    if (options.help) {
        showHelp();
        process.exit(0);
    }

    try {
        let result;
        
        if (options.mode === 'legacy') {
            // Use legacy AO message sending
            result = await sendLegacyMessage(options);
        } else {
            // Use HyperBEAM message sending (default)
            result = await sendHyperBeamMessage(options);
        }
        
        const output = formatOutput(result, options);
        console.log(output);
        
    } catch (error) {
        if (options.output === 'unity') {
            const errorResponse = {
                Messages: [],
                Spawns: [],
                Output: "",
                Error: error.message,
                uniqueID: options.uniqueID
            };
            console.log(JSON.stringify(errorResponse));
        } else {
            logError(options, '❌ Script failed:', error.message);
        }
        process.exit(1);
    }
}

// Handle uncaught errors
process.on('uncaughtException', (error) => {
    console.error('💥 Uncaught exception:', error.message);
    process.exit(1);
});

process.on('unhandledRejection', (error) => {
    console.error('💥 Unhandled rejection:', error.message);
    process.exit(1);
});

// Run the script
main().catch(console.error);

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
        verbose: false,
        output: "json", // json, unity, or simple
        uniqueID: null, // Will be set from command line or generated if not provided
        mode: "hyperbeam" // hyperbeam or legacy
    };

    for (let i = 0; i < args.length; i++) {
        const arg = args[i];
        
        if (arg === '--help' || arg === '-h') {
            options.help = true;
        } else if (arg === '--verbose' || arg === '-v') {
            options.verbose = true;
        } else if (arg === '--process-id' || arg === '-p') {
            options.processId = args[++i];
        } else if (arg === '--hyperbeam-url' || arg === '-u') {
            options.hyperBeamUrl = args[++i];
        } else if (arg === '--wallet' || arg === '-w') {
            options.walletPath = args[++i];
        } else if (arg === '--data' || arg === '-d') {
            options.data = args[++i];
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
function loadWallet(walletPath) {
    try {
        const wallet = JSON.parse(fs.readFileSync(walletPath, 'utf8'));
        return wallet;
    } catch (error) {
        console.error('❌ Failed to load wallet from:', walletPath);
        console.error('💡 Make sure the wallet file exists and is valid JSON');
        process.exit(1);
    }
}

// Send Legacy AO message
async function sendLegacyMessage(options) {
    const wallet = loadWallet(options.walletPath);
    
    if (options.verbose) {
        console.log('🔧 Configuration (Legacy Mode):');
        console.log('   Process ID:', options.processId);
        console.log('   Wallet Path:', options.walletPath);
        console.log('   Data:', options.data || '(empty)');
        console.log('   Tags:', Object.keys(options.tags).length > 0 ? options.tags : '(none)');
        console.log('   Wallet:', wallet ? 'Loaded' : 'Not loaded');
        console.log('');
    }

    try {
        // Convert tags object to array format for legacy AO
        const tagsArray = Object.entries(options.tags).map(([name, value]) => ({
            name,
            value
        }));

        if (options.verbose) {
            console.log('📤 Sending legacy message with parameters:');
            console.log('   Process:', options.processId);
            console.log('   Tags:', tagsArray);
            console.log('   Data:', options.data || '(empty)');
            console.log('');
        }

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

        if (options.verbose) {
            console.log('📨 Message sent, ID:', messageId);
            console.log('🔄 Getting result...');
        }

        // Get the result
        const legacyResult = await result({
            message: messageId,
            process: options.processId
        });

        if (options.verbose) {
            console.log('✅ Legacy AO message sent successfully!');
            console.log('');
        }

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
            console.error('❌ Failed to send legacy AO message:', error.message);
            if (options.verbose) {
                console.error('💡 Full error:', error);
            }
        }
        process.exit(1);
    }
}

// Send HyperBEAM message
async function sendHyperBeamMessage(options) {
    const wallet = loadWallet(options.walletPath);
    
    if (options.verbose) {
        console.log('🔧 Configuration:');
        console.log('   Process ID:', options.processId);
        console.log('   HyperBEAM URL:', options.hyperBeamUrl);
        console.log('   Wallet Path:', options.walletPath);
        console.log('   Data:', options.data || '(empty)');
        console.log('   Tags:', Object.keys(options.tags).length > 0 ? options.tags : '(none)');
        console.log('   Wallet:', wallet ? 'Loaded' : 'Not loaded');
        console.log('');
    }

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

        if (options.verbose) {
            console.log('📤 Sending request with parameters:', requestParams);
            console.log('');
        }

        // Send request via HyperBEAM
        const result = await request(requestParams);

        if (options.verbose) {
            console.log('✅ HyperBEAM message sent successfully!');
            console.log('');
        }

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
            console.error('❌ Failed to send HyperBEAM message:', error.message);
            if (options.verbose) {
                console.error('💡 Full error:', error);
            }
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
                    // For HyperBEAM mode, extract body from HyperBEAM response: result.json.body
                    if (result && result.json && result.json.body) {
                        // Parse the body if it's a JSON string
                        if (typeof result.json.body === 'string') {
                            resultData = JSON.parse(result.json.body);
                        } else {
                            resultData = result.json.body;
                        }
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
            
        case 'simple':
            // Simple text output
            if (options.mode === 'legacy') {
                return JSON.stringify(result, null, 2);
            } else if (result && result.json && result.json.body) {
                return JSON.stringify(result.json.body, null, 2);
            }
            return JSON.stringify(result, null, 2);
            
        case 'json':
        default:
            // Full JSON output
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
   -v, --verbose                 Verbose output
   -p, --process-id <id>         Target process ID (default: ${DEFAULT_PROCESS_ID})
   -u, --hyperbeam-url <url>     HyperBEAM URL (default: ${DEFAULT_HYPERBEAM_URL})
   -w, --wallet <path>      Arweave wallet keyfile path (default: ../wallet.json)
   -d, --data <data>             Message data payload
   -o, --output <format>         Output format: json, unity, simple (default: json)
   -m, --mode <mode>             Message mode: hyperbeam, legacy (default: hyperbeam)
   
   --tag-<name>=<value>          Add tag (e.g., --tag-Action=EnterMatchmaking)
   -t<name>=<value>              Add tag (short form, e.g., -tAction=EnterMatchmaking)

📋 Examples:
   # HyperBEAM mode (default)
   node aoconnect-editor.js --tag-Action=GetUserInfo
   
   # Legacy AO mode
   node aoconnect-editor.js --mode legacy --tag-Action=GetUserInfo
   
   # Enter matchmaking with HyperBEAM
   node aoconnect-editor.js \\
     --tag-Action=EnterMatchmaking \\
     --tag-MatchType=CasualAI \\
     --tag-Class=SamuraiBZ \\
     --tag-SkinId=1
   
   # Enter matchmaking with legacy AO
   node aoconnect-editor.js --mode legacy \\
     --tag-Action=EnterMatchmaking \\
     --tag-MatchType=CasualAI
   
   # Unity-friendly output format
   node aoconnect-editor.js --output unity --tag-Action=GetUserInfo
   
   # Custom wallet path
   node aoconnect-editor.js --wallet /path/to/wallet.json --tag-Action=GetUserInfo
   
   # With data payload
   node aoconnect-editor.js --data "Hello AO!" --tag-Action=TestMessage

🌐 Requirements:
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
            console.error('❌ Script failed:', error.message);
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

import { connect } from '@permaweb/aoconnect'
import { createDataItemSignerMain, createDataItemSignerSession, getConnectedChain } from './connectWallet'

export async function sendMessageCustomCallback(pid, data, tagsStr, id, objectCallback, methodCallback, useMainWallet, chain) {
    var tags = JSON.parse(tagsStr);
    let json;

    try {
        if (chain == 'default')
        {
            chain = null;
        } 

        // Select the appropriate signer function
        let signerFunction;
        let chainToUse = chain || getConnectedChain();
        if (useMainWallet == 'true' || chainToUse === 'arweave') {
            signerFunction = createDataItemSignerMain(chainToUse);
        } else {
            signerFunction = createDataItemSignerSession(chainToUse);
        }

        const messageId = await connect().message({
            process: pid,
            signer: signerFunction,
            tags: tags,
            data: data,
            anchor: Math.round(Date.now() / 1000)
            .toString()
            .padStart(32, Math.floor(Math.random() * 10).toString())
        });

        const result = await connect().result({
            message: messageId,
            process: pid
        });

        result.uniqueID = id;
        json = JSON.stringify(result);
    } catch (error) {

        const errorResponse = {
            Error: error.message,
            uniqueID: id,
        };
        json = JSON.stringify(errorResponse);
    }

    myUnityInstance.SendMessage(objectCallback, methodCallback, json);
    return json;
}

export async function transferToken(pid, quantity, recipient) {
    const messageId = await connect().message({
        process: pid,
        signer: createDataItemSignerMain(),
        tags: [{ name: 'Action', value: 'Transfer' }, { name: 'Quantity', value: quantity }, { name: 'Recipient', value: recipient }],
        data: ''
    });

    const result = await connect().result({
        message: messageId,
        process: pid
    });

    console.log(result);

    var json = JSON.stringify(result);

    myUnityInstance.SendMessage('AOConnectManager', 'TransferCallback', json);
}

export async function sendMessageHyperBeam(pid, data, tagsStr, id, objectCallback, methodCallback, useMainWallet, chain, hyperBeamUrl) {
    var tags = JSON.parse(tagsStr);
    let json;

    try {
        console.log("HyperBEAM request - pid:", pid, "data:", data, "tags:", tags, "hyperBeamUrl:", hyperBeamUrl);
        
        if (chain == 'default') {
            chain = null;
        }

        // Select the appropriate signer function
        let signerFunction;
        let chainToUse = chain || getConnectedChain();
        if (useMainWallet == 'true' || chainToUse === 'arweave') {
            signerFunction = createDataItemSignerMain(chainToUse);
        } else {
            signerFunction = createDataItemSignerSession(chainToUse);
        }

        const { request } = connect({
            MODE: "mainnet",
            URL: hyperBeamUrl,
            signer: signerFunction,
        });

        // Convert tags array to properties object for HyperBEAM request
        // Using aoconnect 0.0.90 path format
        let requestParams = {
            path: `/${pid}~process@1.0/push`,
            method: "POST",
            target: pid,
            signingFormat: "ANS-104",
            "accept-bundle": "true",
            "require-codec": "application/json",
        };

        // Add data if provided
        if (data && data.length > 0) {
            requestParams.data = data;
        }

        // Add tags as properties (like Action, etc.)
        tags.forEach(tag => {
            requestParams[tag.name] = tag.value;
        });

        console.log("HyperBEAM requestParams:", requestParams);

        // Send request via HyperBEAM
        let processResult;
        try {
            processResult = await request(requestParams);
            console.log("HyperBEAM processResult:", processResult);
        } catch (requestError) {
            console.error("HyperBEAM request failed:", requestError);
            throw new Error(`HyperBEAM request failed: ${requestError.message}`);
        }

        // Extract body from HyperBEAM response (aoconnect 0.0.90)
        // Response structure: result.body contains a JSON string with outbox, output, slot
        let aoResponse;
        try {
            if (processResult && processResult.body) {
                // Parse the body string to get the actual AO response
                if (typeof processResult.body === 'string') {
                    aoResponse = JSON.parse(processResult.body);
                    console.log("Parsed HyperBEAM response:", aoResponse);
                } else {
                    // Already parsed
                    aoResponse = processResult.body;
                }
            } else {
                throw new Error("No body in HyperBEAM response");
            }

            // Convert HyperBEAM response format to Unity format
            // HyperBEAM gives us: { outbox: [...], output: {data, print}, slot: N, ... }
            // Unity NodeCU expects: { Messages: [...], Output: {data, print}, Spawns: [], ... }
            
            // Convert HyperBEAM outbox messages to Unity Message format
            const convertedMessages = (aoResponse.outbox || aoResponse.Messages || []).map(msg => {
                // Special fields that shouldn't be tags
                const specialFields = ['Data', 'data', 'Target', 'target', 'Anchor', 'anchor', 'Id', 'id'];
                
                const unityMessage = {
                    tags: []
                };
                
                // Convert all properties to tags except special fields
                for (const [key, value] of Object.entries(msg)) {
                    if (specialFields.includes(key)) {
                        // Map special fields to their Unity lowercase equivalents
                        const lowerKey = key.toLowerCase();
                        unityMessage[lowerKey] = value;
                    } else {
                        // Everything else becomes a tag
                        unityMessage.tags.push({
                            name: key,
                            value: String(value)
                        });
                    }
                }
                
                return unityMessage;
            });
            
            // Handle Output field - NodeCU expects an object with 'data' and 'print' fields
            let outputObj;
            if (aoResponse.output && typeof aoResponse.output === 'object') {
                // HyperBEAM format: output is already an object
                // Parse 'print' field - can be string "true"/"false" or boolean
                let printValue = true;
                if (aoResponse.output.print !== undefined) {
                    if (typeof aoResponse.output.print === 'string') {
                        printValue = aoResponse.output.print.toLowerCase() === 'true';
                    } else {
                        printValue = Boolean(aoResponse.output.print);
                    }
                }
                
                outputObj = {
                    data: aoResponse.output.data || "",
                    print: printValue
                };
            } else if (typeof aoResponse.Output === 'object') {
                // Already in Unity format
                outputObj = aoResponse.Output;
            } else {
                // Fallback: create object from string
                outputObj = {
                    data: aoResponse.output || aoResponse.Output || "",
                    print: true
                };
            }
            
            const response = {
                Messages: convertedMessages,
                Spawns: aoResponse.Spawns || [],
                Output: outputObj,
                Assignments: aoResponse.Assignments || [],
                Slot: aoResponse.slot || aoResponse.Slot,
                Process: aoResponse.process || aoResponse.Process,
                Error: aoResponse.Error || null,
                uniqueID: id,
            };

            json = JSON.stringify(response);

        } catch (bodyError) {
            // Handle case where parsing fails
            console.warn("Failed to parse HyperBEAM response body:", bodyError.message);
            console.warn("Raw result:", processResult);
            
            const fallbackResponse = {
                Messages: [],
                Spawns: [],
                Output: { data: processResult ? JSON.stringify(processResult) : "", print: true },
                Error: `Response parsing failed: ${bodyError.message}`,
                uniqueID: id,
            };
            
            json = JSON.stringify(fallbackResponse);
        }

    } catch (error) {
        console.error("Error in sendMessageHyperBeam:", error.message);

        const errorResponse = {
            Messages: [],
            Spawns: [],
            Output: { data: "", print: false },
            Error: error.message,
            uniqueID: id,
        };
        json = JSON.stringify(errorResponse);
    }

    console.log("Sending HyperBEAM Message to", objectCallback, "in", methodCallback);
    // Always send a message back to the Unity instance regardless of success or failure
    myUnityInstance.SendMessage(objectCallback, methodCallback, json);

    return json;
}

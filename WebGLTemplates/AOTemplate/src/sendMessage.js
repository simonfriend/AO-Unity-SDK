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
        let requestParams = {
            path: `/${pid}~process@1.0/push/serialize~json@1.0`,
            method: "POST",
            target: pid,
            signingFormat: "ANS-104",
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

        // Extract body from HyperBEAM response: body.json.body
        let resultData;
        try {
            if (processResult && processResult.json && processResult.json.body) {
                resultData = processResult.json.body;
            } else if (processResult) {
                // Fallback to full result if body structure is different
                resultData = processResult;
            } else {
                throw new Error("Empty response from HyperBEAM");
            }

            const response = {
                Messages: resultData.Messages || [],
                Spawns: resultData.Spawns || [],
                Output: resultData.Output || "",
                Error: resultData.Error || null,
                uniqueID: id,
            };

            json = JSON.stringify(response);

        } catch (bodyError) {
            // Handle case where body.json.body is null or malformed
            console.warn("Failed to parse HyperBEAM response body:", bodyError.message);
            
            const fallbackResponse = {
                Messages: [],
                Spawns: [],
                Output: processResult ? JSON.stringify(processResult) : "",
                Error: `Body parsing failed: ${bodyError.message}`,
                uniqueID: id,
            };
            
            json = JSON.stringify(fallbackResponse);
        }

    } catch (error) {
        //console.error("Error in sendMessageHyperBeam:", error.message);

        const errorResponse = {
            Messages: [],
            Spawns: [],
            Output: "",
            Error: error.message,
            uniqueID: id,
        };
        json = JSON.stringify(errorResponse);
    }

    //console.log("Sending HyperBEAM Message to", objectCallback, "in", methodCallback);
    // Always send a message back to the Unity instance regardless of success or failure
    myUnityInstance.SendMessage(objectCallback, methodCallback, json);

    return json;
}

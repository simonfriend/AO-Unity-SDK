import { connect } from '@permaweb/aoconnect'
import { createDataItemSignerMain, createDataItemSignerSession, getConnectedChain } from './connectWallet'

export async function sendMessageCustomCallback(pid, data, tagsStr, id, objectCallback, methodCallback, useMainWallet) {
    var tags = JSON.parse(tagsStr);
    let json;

    try {
        // Select the appropriate signer function
        let signerFunction;
        console.log("useMainWallet", useMainWallet);
        if (useMainWallet == 'true' || getConnectedChain() == 'arweave') {
            signerFunction = createDataItemSignerMain();
        } else {
            signerFunction = createDataItemSignerSession();
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
        //console.error("Error in sendMessage:", error.message);

        const errorResponse = {
            Error: error.message,
            uniqueID: id,
        };
        json = JSON.stringify(errorResponse);
    }

    //console.log("Sending Message to", objectCallback, "in", methodCallback);
    // Always send a message back to the Unity instance regardless of success or failure
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

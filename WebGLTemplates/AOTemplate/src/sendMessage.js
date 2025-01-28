import { connect } from '@permaweb/aoconnect'
import { createDataItemSigner } from './connectWallet'

export async function sendMessageCustomCallback(pid, data, tagsStr, id, objectCallback, methodCallback) {
    var tags = JSON.parse(tagsStr);
    let json;

    try {
        const messageId = await connect().message({
            process: pid,
            signer: createDataItemSigner(),
            tags: tags,
            data: data
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
        signer: createDataItemSigner(),
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

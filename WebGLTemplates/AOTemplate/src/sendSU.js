// import { createDataItemSigner } from '@permaweb/aoconnect'
import { createDataItemSignerMain, getArweaveSigner } from './connectWallet'
// import WarpArBundles from 'warp-arbundles'
// const { createData } = WarpArBundles;

//url: https://su-router.ao-testnet.xyz
export async function sendSU(url, target, owner, action, data/*, objectCallback, methodCallback*/) {

    var activeSigner = await getArweaveSigner();
    var activeAddress = await activeSigner.getActiveAddress();
    // var activeAddress = await getActiveAddress();

    if (activeAddress != owner) {
        console.log('Error, owner:', owner);
        return false;
    }

    const signer = createDataItemSignerMain(activeSigner)

    const tags = [
        // { name: 'Target', value: target },
        { name: 'Action', value: action },
        { name: 'Data-Protocol', value: 'ao' },
        { name: 'Type', value: 'Message' },
        { name: 'Variant', value: 'ao.TN.1' },
        // { name: 'Module', value: 'Isk_GYo30Tyf5nLbVI6zEJIfFpiXQJd58IKcIkTu4no' },
        // { name: 'Scheduler', value: '_GQ33BkPtZrqxA84vM8Zk-N2aO0toNNu_C-l-rawrBA' }
    ]

    // const dataItem = createData(data, signer, { tags: tags })
    // await dataItem.sign(signer)
    // const dataItem = signer({ data, tags })
    const dataItem = await signer({data, tags, target: target})

    const response = await fetch(
        url,
        {
            method: 'POST',
            headers: {
                'Content-Type': 'application/octet-stream',
                Accept: 'application/json'
            },
            body: dataItem.raw
        }
    )

    const responseText = await response.text()

    myUnityInstance.SendMessage("AONetworkManager", "CallbackSU", responseText);
}

import { transferToken, sendMessageCustomCallback } from './sendMessage.js'
import { spawnProcess, fetchProcesses } from './spawnProcess.js'
import { connectArweaveWallet, connectMetamaskWallet } from './connectWallet.js'
import { requestNotificationPermission, sendNotification } from './utils.js'
import { downloadImage, shareOnTwitter } from './screenshot.js'
import { localCuEvaluate, localCuRegister } from './localCu.js'
import { sendSU } from './sendSU.js'

export const UnityAO = {
    sendMessageCustomCallback,
    transferToken,
    spawnProcess,
    connectArweaveWallet,
    connectMetamaskWallet,
    fetchProcesses,
    requestNotificationPermission,
    sendNotification,
    downloadImage,
    shareOnTwitter,
    localCuRegister,
    localCuEvaluate,
    sendSU
}

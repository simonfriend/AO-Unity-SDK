import { transferToken, sendMessageCustomCallback } from './sendMessage.js'
import { spawnProcess, fetchProcesses } from './spawnProcess.js'
import { connectArweaveWallet, connectMetamaskWallet, signOutWander, openWanderConnect, closeWanderConnect } from './connectWallet.js'
import { requestNotificationPermission, sendNotification } from './utils.js'
import { downloadImage, shareOnTwitter } from './screenshot.js'
import { localCuEvaluate, localCuRegister } from './localCu.js'
import { sendSU } from './sendSU.js'
import { stake, unstake, getTokenBalance, getStakedBalance } from './stakeHandler.js'

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
    sendSU,
    stake,
    unstake,
    getTokenBalance,
    getStakedBalance,
    signOutWander,
    openWanderConnect,
    closeWanderConnect
}

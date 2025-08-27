import { Web3Provider } from '@ethersproject/providers'
import { EthereumSigner, InjectedEthereumSigner } from '@dha-team/arbundles'
import { ethers } from 'ethers';
import { WanderConnect } from "@wanderapp/connect";
import { createDataItemSigner } from '@permaweb/aoconnect'

let wanderAuthType = null;
let wanderWalletLoaded = false;
let firstOnAuth = true;

// Initialize Wander Connect:
const wander = new WanderConnect({
    button: false,
    clientId: "FREE_TRIAL",
    onAuth: (userDetails) => {
        if (!!userDetails) {
            try {
                console.log("Wander Connect user details:", userDetails);

                // Sign out on first onAuth to clear any previous sessions
                if (firstOnAuth) {
                    try {
                        wander.signOut();
                        console.log('Cleared previous Wander session on first onAuth');
                    } catch (error) {
                        console.warn('Could not clear previous Wander session:', error);
                    }
                    firstOnAuth = false;
                    return;
                }

                if (userDetails.authStatus === 'authenticated') {
                    wanderAuthType = userDetails.authType;
                    connectArweaveWallet();
                }
                else if (userDetails.authType === 'NATIVE_WALLET') {
                    wanderAuthType = userDetails.authType;
                    connectArweaveWallet();
                }
                else if (userDetails.authStatus === 'loading') {
                    wanderAuthType = userDetails.authType;
                    myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Loading');
                }

                firstOnAuth = false; // Reset firstOnAuth after handling the first auth

            } catch (error) {
                console.error("Error handling Wander auth:", error);
            }
        }
    }
});

// Listen for the arweaveWalletLoaded event to know when Wander wallet is ready
window.addEventListener("arweaveWalletLoaded", async (e) => {
    try {
        console.log("Arweave wallet loaded event - Wander is ready:", e.detail);
        wanderWalletLoaded = true;
    } catch (error) {
        console.error("Error handling arweaveWalletLoaded:", error);
    }
});


let registeredEvent = false;
let registeredMetamaskEvent = false;

let connectedChains = [];

// Store both wallet infos
let arweaveWalletInfo = null;
let evmWalletInfo = null;

export function getWalletInfo(chain) {
    if (chain === 'arweave') return arweaveWalletInfo;
    if (chain === 'evm') return evmWalletInfo;
    return null;
}

export async function getArweaveSigner() {
    if (globalThis.arweaveWallet) {
        return globalThis.arweaveWallet;
    }
}

function addConnectedChain(chain) {
    if (!connectedChains.includes(chain)) {
        connectedChains.push(chain);
    }
}

export function getConnectedChain(index = 0) {
    return connectedChains.length > index ? connectedChains[index] : null;
}

export function openWanderConnect() {
    if (wander) {
        wander.open();
    } else {
        console.error("Wander Connect is not initialized.");
    }
}

export function closeWanderConnect() {
    if (wander) {
        wander.close();
    } else {
        console.error("Wander Connect is not initialized.");
    }
}

export function signOutWander() {
    if (wander && typeof wander.signOut === 'function') {
        try {
            wander.signOut();
            wanderAuthType = null;
            console.log('Wander signed out manually');
        } catch (error) {
            console.error('Error signing out from Wander:', error);
        }
    } else {
        console.error("Wander Connect is not initialized or signOut method not available.");
    }
}

export async function connectArweaveWallet() {
    if (wander) {
        if (wanderAuthType == null) {
            openWanderConnect();
            return;
        }
    }

    if (!globalThis.arweaveWallet) {
        alert('Error: No Arweave wallet available! Please connect through Wander Connect or install Wander extension.');
        return;
    }

    try {
        await globalThis.arweaveWallet.connect([
            'ACCESS_PUBLIC_KEY', 'SIGNATURE', 'ACCESS_ADDRESS', 'ACCESS_ALL_ADDRESSES', 'SIGN_TRANSACTION'
        ]);
    } catch (error) {
        console.error('Error connecting wallet:', error);
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Error');
        return null;
    }

    addConnectedChain('arweave');

    if (!registeredEvent) {
        addEventListener("walletSwitch", async (e) => {
            const newAddress = e.detail.address;
            console.log("New Address: " + newAddress);
            const allNewAddresses = await window.arweaveWallet.getAllAddresses();
            const addressInfo = {
                address: newAddress,
                addresses: allNewAddresses
            };
            const addressInfoString = JSON.stringify(addressInfo);
            myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', addressInfoString);
        });
        registeredEvent = true;
    }

    try {
        var activeAddress = await globalThis.arweaveWallet.getActiveAddress();
        const allAddresses = await window.arweaveWallet.getAllAddresses();
        const addressInfo = {
            address: activeAddress,
            addresses: allAddresses,
            chain: 'arweave'
        };
        arweaveWalletInfo = addressInfo;
        const addressInfoString = JSON.stringify(addressInfo);
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', addressInfoString);
    }
    catch (error) {
        console.error('Error checking active address:', error);
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Error');
    }
}


export async function connectMetamaskWallet() {
    if (!window.ethereum) {
        alert('Error: No MetaMask extension installed!');
        return;
    }
    try {
        // Request account access
        const accounts = await window.ethereum.request({ method: 'eth_requestAccounts' });
        addConnectedChain('evm');
        const mainAccount = accounts[0];
        const storageKey = `ethSessionKey_${mainAccount}`;
        const now = Date.now();
        let sessionData = null;
        const stored = localStorage.getItem(storageKey);
        if (stored) {
            try {
                sessionData = JSON.parse(stored);
            } catch (e) {
                console.warn("Invalid session key data in storage, ignoring.");
                sessionData = null;
            }
        }
        const ONE_DAY_MS = 24 * 60 * 60 * 1000; // To avoid risk that while people are playing the session key expires
        if (!sessionData || !sessionData.privateKey || now + ONE_DAY_MS > sessionData.expiry ||
            (sessionData.mainAccount && sessionData.mainAccount.toLowerCase() !== mainAccount.toLowerCase())) {
            sessionData = generateSessionKey(mainAccount);
            if (!sessionData) {
                throw new Error("Failed to generate Ethereum session key");
            }
            localStorage.setItem(storageKey, JSON.stringify(sessionData));
            console.log("New session key generated:", sessionData.address);
        } else {
            console.log("Reusing existing session key:", sessionData.address);
        }

        // Build addressInfo with sessionKey details
        const addressInfo = {
            address: mainAccount,
            addresses: accounts,
            sessionKey: {
                address: sessionData.address,
                mainWallet: mainAccount,
                expiry: sessionData.expiry
            },
            chain: 'evm'
        };
        evmWalletInfo = addressInfo;
        const addressInfoString = JSON.stringify(addressInfo);
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', addressInfoString);

        // Register account change listener (invalidate session key on change)
        if (!registeredMetamaskEvent) {
            window.ethereum.on('accountsChanged', (accounts) => {
                if (accounts.length === 0) {
                    console.log('MetaMask is locked or no accounts connected');
                    myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Error');
                    return;
                }
                const newAccount = accounts[0];
                const newStorageKey = `ethSessionKey_${newAccount}`;
                let newSessionData = null;
                const storedNew = localStorage.getItem(newStorageKey);
                if (storedNew) {
                    try {
                        newSessionData = JSON.parse(storedNew);
                    } catch (e) {
                        newSessionData = null;
                    }
                }
                const ONE_DAY_MS = 24 * 60 * 60 * 1000; // To avoid risk that while people are playing the session key expires
                if (!newSessionData || !newSessionData.privateKey || Date.now() + ONE_DAY_MS > newSessionData.expiry ||
                    (newSessionData.mainAccount && newSessionData.mainAccount.toLowerCase() !== newAccount.toLowerCase())) {
                    newSessionData = generateSessionKey(newAccount);
                    localStorage.setItem(newStorageKey, JSON.stringify(newSessionData));
                }
                const newAddressInfo = {
                    address: newAccount,
                    addresses: accounts,
                    sessionKey: {
                        address: newSessionData.address,
                        mainWallet: newAccount,
                        expiry: newSessionData.expiry
                    },
                    chain: 'evm'
                };
                evmWalletInfo = newAddressInfo;
                myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', JSON.stringify(newAddressInfo));
            });
            registeredMetamaskEvent = true;
        }
        return mainAccount;
    } catch (error) {
        console.error('Error connecting wallet:', error.message || error);
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Error');
        return null;
    }
}

function generateSessionKey(mainAccount) {
    const ONE_WEEK_MS = 7 * 24 * 60 * 60 * 1000;
    try {
        if (typeof ethers !== "undefined") {
            const wallet = ethers.Wallet.createRandom();
            return {
                privateKey: wallet.privateKey,
                address: wallet.address,
                mainAccount: mainAccount,
                expiry: Date.now() + ONE_WEEK_MS
            };
        } else {
            console.warn("No Web3/Ethers library found. Fallback key generation not fully implemented.");
            const randomBytes = new Uint8Array(32);
            window.crypto.getRandomValues(randomBytes);
            const privateKeyHex = "0x" + Array.from(randomBytes).map(b => b.toString(16).padStart(2, "0")).join("");
            return {
                privateKey: privateKeyHex,
                address: "(derivation needed)",
                mainAccount: mainAccount,
                expiry: Date.now() + ONE_WEEK_MS
            };
        }
    } catch (err) {
        console.error("Error generating session key:", err);
        return null;
    }
}

// For signing with the main MetaMask wallet
export const createDataItemSignerMain = (chain = getConnectedChain()) => {
    if (chain === 'arweave') {
        return createDataItemSigner(globalThis.arweaveWallet);
    } else if (chain === 'evm') {
        return async (create, signatureType) => {
            const provider = new Web3Provider(window.ethereum);
            const ethSigner = new InjectedEthereumSigner(provider);
            await ethSigner.setPublicKey();
            
            const dataToSign = await create({
                publicKey: ethSigner.publicKey, // 65-byte secp256k1 public key (Uint8Array)
                type: 3, // SignatureConfig.ETHEREUM from arbundles constants
                alg: "secp256k1"
            });
            
            // Sign the data with our Ethereum signer
            const signature = await ethSigner.sign(dataToSign);
            
            return {
                signature: signature,
                owner: ethSigner.publicKey 
            };
        };
    } else {
        throw new Error(`Unsupported chain type: ${chain}`);
    }
};

// For signing with the session key
export const createDataItemSignerSession = (chain = getConnectedChain()) => {
    if (chain === 'evm') {
        const privateKey = tryGetSessionPrivateKey();
        if (!privateKey) {
            throw new Error('No session key available');
        }
        
        return async (create, signatureType) => {
            const arbundlesSigner = new EthereumSigner(privateKey);
            
            const publicKeyBuffer = arbundlesSigner.publicKey; // Buffer
            const publicKeyUint8 = new Uint8Array(publicKeyBuffer); // Convert to Uint8Array
            
            const dataToSign = await create({
                publicKey: publicKeyUint8, // 65-byte secp256k1 public key (Uint8Array)
                type: 3, // SignatureConfig.ETHEREUM from arbundles constants
                alg: "secp256k1"
            });
            
            const signature = await arbundlesSigner.sign(dataToSign);
            
            return {
                signature: signature,
                owner: publicKeyUint8 // Uint8Array
            };
        };
    } else {
        throw new Error(`Unsupported chain type for session: ${chain}`);
    }
};

function tryGetSessionPrivateKey() {
    const mainAccount = window.ethereum.selectedAddress;
    if (!mainAccount) {
        throw new Error('No MetaMask account available for session key signing');
    }
    const storageKey = `ethSessionKey_${mainAccount}`;
    const stored = localStorage.getItem(storageKey);
    if (!stored) {
        throw new Error('No session key available');
    }
    let sessionData;
    try {
        sessionData = JSON.parse(stored);
    } catch (e) {
        throw new Error('Invalid session key data');
    }
    return sessionData.privateKey;
}

// import { createDataItemSigner } from '@permaweb/aoconnect'
import { createAoSigner } from '@ar.io/sdk'
import { Web3Provider } from '@ethersproject/providers'
// import { InjectedEthereumSigner } from 'arseeding-arbundles/src/signing'
// import { DataItem, createData } from 'arseeding-arbundles'
import { EthereumSigner,InjectedEthereumSigner, DataItem, createData } from '@dha-team/arbundles'
import { ethers } from 'ethers';

let registeredEvent = false;
let registeredMetamaskEvent = false;

let connectedChain = 'arweave';

export async function getArweaveSigner() {
    if (globalThis.arweaveWallet) {
        return globalThis.arweaveWallet;
    }
}

export function getConnectedChain() {
    return connectedChain;
}

export async function connectArweaveWallet() {
    if (!globalThis.arweaveWallet) {
        alert('Error: No Arconnect extension installed!');
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

    connectedChain = 'arweave';

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
            addresses: allAddresses
        };
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
        connectedChain = 'ethereum';
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
            }
        };
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
                    }
                };
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
        // if (typeof Web3 !== "undefined") {
        //     const web3 = new Web3();
        //     const newAccount = web3.eth.accounts.create();
        //     return {
        //         privateKey: newAccount.privateKey,
        //         address: newAccount.address,
        //         mainAccount: mainAccount,
        //         expiry: Date.now() + ONE_WEEK_MS
        //     };
        // } else 
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
export const createDataItemSignerMain = () => {
    if (connectedChain === 'arweave') {
        return async ({ data, tags = [], target, anchor } = {}) => {
            const signed = await window.arweaveWallet.signDataItem({ data, tags, anchor, target });
            const dataItem = new DataItem(Buffer.from(signed));
            return {
                id: await dataItem.id,
                raw: await dataItem.getRaw()
            };
        }
    } else if (connectedChain === 'ethereum') {
        return async ({ data, tags = [], target, anchor } = {}) => {
            const provider = new Web3Provider(window.ethereum);
            const signer = new InjectedEthereumSigner(provider);
            await signer.setPublicKey();
            const dataItem = createData(data, signer, { tags, target, 
                anchor: Math.round(Date.now() / 1000).toString().padStart(32, Math.floor(Math.random() * 10).toString()) });
            await dataItem.sign(signer);
            return {
                id: dataItem.id,
                raw: dataItem.getRaw()
            };
        }
    } else {
        throw new Error(`Unsupported chain type: ${connectedChain}`);
    }
};

// For signing with the session key
export const createDataItemSignerSession = () => {
    if (connectedChain === 'ethereum') {
        const privateKey = tryGetSessionPrivateKey();
        if (!privateKey) {
            throw new Error('No session key available');
        }
        const arbundlesSigner = new EthereumSigner(privateKey)
        return createAoSigner(arbundlesSigner)
    } else {
        throw new Error(`Unsupported chain type for session: ${connectedChain}`);
    }
};

// export const createDataItemSignerSession = () => {
//     if (connectedChain === 'arweave') {
//         return async ({ data, tags = [], target, anchor } = {}) => {
//             const signed = await window.arweaveWallet.signDataItem({ data, tags, anchor, target });
//             const dataItem = new DataItem(Buffer.from(signed));
//             return {
//                 id: await dataItem.id,
//                 raw: await dataItem.getRaw()
//             };
//         }
//     } else if (connectedChain === 'ethereum') {
//         return async ({ data, tags = [], target, anchor } = {}) => {
//             // const privateKey = tryGetSessionPrivateKey();
//             // const arbundlesSigner = new EthereumSigner(privateKey)
//             const sessionSigner = createSessionEthereumSigner(); 
//             const dataItem = createData(data, sessionSigner, { tags, target, anchor });
//             await dataItem.sign(sessionSigner);
//             return {
//                 id: dataItem.id,
//                 raw: dataItem.getRaw()
//             };
//         }
//     } else {
//         throw new Error(`Unsupported chain type: ${connectedChain}`);
//     }
// };

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

// function createSessionEthereumSigner() {
//     const mainAccount = window.ethereum.selectedAddress;
//     if (!mainAccount) {
//         throw new Error('No MetaMask account available for session key signing');
//     }
//     const storageKey = `ethSessionKey_${mainAccount}`;
//     const stored = localStorage.getItem(storageKey);
//     if (!stored) {
//         throw new Error('No session key available');
//     }
//     let sessionData;
//     try {
//         sessionData = JSON.parse(stored);
//     } catch (e) {
//         throw new Error('Invalid session key data');
//     }

//     const wallet = new ethers.Wallet(sessionData.privateKey);

//     return {
//         // Match ARBundles' expected public key format (64-byte raw public key)
//         publicKey: Buffer.from(wallet.signingKey.publicKey.slice(4), 'hex'), // Remove 0x04 prefix
//         address: wallet.address,
//         async signMessage(message) {
//             // Replicate eth_sign behavior: sign raw Keccak-256 hash
//             const msgHex = ethers.hexlify(message);
//             const msgHash = ethers.keccak256(msgHex);
//             const sig = wallet.signingKey.sign(msgHash);
//             return sig.serialized;
//         },
//         async setPublicKey() {
//             // ARBundles requires this method but doesn't need implementation here
//         }
//     };
// }

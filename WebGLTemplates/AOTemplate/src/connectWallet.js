// import { createDataItemSigner } from '@permaweb/aoconnect'
import { Web3Provider } from '@ethersproject/providers'
import { InjectedEthereumSigner } from 'arseeding-arbundles/src/signing'
import { DataItem } from 'arseeding-arbundles'
import { createData } from 'arseeding-arbundles'
//import * as othentSigner from '@othent/kms'
//import { isVouched } from 'vouchdao'

let registeredEvent = false;
let registeredMetamaskEvent = false;

let connectedChain = 'arweave';

export async function getArweaveSigner() 
{
    if (globalThis.arweaveWallet) {
        return globalThis.arweaveWallet;
    }
    //else {
    //    const signer = Object.assign({}, othentSigner, {
    //        getActiveAddress: () => othentSigner.getActiveKey(),
    //        getAddress: () => othentSigner.getActiveKey(),
    //        signer: (tx) => othentSigner.sign(tx),
    //        type: 'arweave'
    //    });
    //    return signer;
    //}
}

export async function getActiveAddress() 
{
    if (connectedChain === 'arweave') 
    {
        return await getArweaveSigner().getActiveAddress();
    } 
    else if (connectedChain === 'ethereum') 
    {
        if (window.ethereum) 
        {
            try {
                // Request account access if needed
                const accounts = await window.ethereum.request({ method: 'eth_requestAccounts' });
        
                // Get the active account (first account in the array)
                const activeAccount = accounts[0];
        
                console.log('Active Account:', activeAccount);
        
                return activeAccount;
            } catch (error) {
                console.error('Error retrieving account:', error);
                return null;
            }
        } 
        else 
        {
            console.error('MetaMask is not installed or window.ethereum is not available');
            return null;
        }
    } 
    else 
    {
        return null;
    }
}

export async function connectArweaveWallet()
{
    if (!globalThis.arweaveWallet) {
        alert('Error: No Arconnect extension installed!');
        return;
    }

    try {
        await globalThis.arweaveWallet.connect(['ACCESS_PUBLIC_KEY', 'SIGNATURE', 'ACCESS_ADDRESS', 'ACCESS_ALL_ADDRESSES', 'SIGN_TRANSACTION']);
    } catch (error) {
        console.error('Error connecting wallet:', error);
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Error');

        return null;
    }

    connectedChain = 'arweave';

    if (!registeredEvent)
    {
        addEventListener("walletSwitch", async (e) => {
            const newAddress = e.detail.address;
            console.log("New Address: " + newAddress);
            
            const allNewAddresses = await window.arweaveWallet.getAllAddresses();
            //console.log(allNewAddresses)
            //const vouched = await isVouched(newAddress)

            // Create a JSON object with address and isVouched keys
            const addressInfo = {
                address: newAddress,
                addresses: allNewAddresses
            };

            // Convert the JSON object to a string
            const addressInfoString = JSON.stringify(addressInfo);

            // Pass the JSON string to Unity
            myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', addressInfoString);

        });
        registeredEvent = true;
    }

    try
    {
        var activeAddress = await globalThis.arweaveWallet.getActiveAddress();

        const allAddresses = await window.arweaveWallet.getAllAddresses();
        //var activeVouched = await isVouched(activeAddress)
        //console.log(allAddresses)

        // Create a JSON object with address and isVouched keys
        const addressInfo = {
            address: activeAddress,
            addresses: allAddresses
        };

        // Convert the JSON object to a string
        const addressInfoString = JSON.stringify(addressInfo);

        // Pass the JSON string to Unity
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', addressInfoString);
    }
    catch (error)
    {
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

        // Send the initial account and all accounts to Unity
        updateUnityWithAccounts(accounts);

        // Register the event listener for account changes
        if (!registeredMetamaskEvent) {
            window.ethereum.on('accountsChanged', handleAccountsChanged);
            registeredMetamaskEvent = true;
        }

        return accounts[0]; // Return the first account if needed
    } catch (error) {
        console.error('Error connecting wallet:', error.message || error);
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Error');
        return null;
    }
}

function handleAccountsChanged(accounts) {
    if (accounts.length === 0) {
        console.log('MetaMask is locked or the user has not connected any accounts');
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Error');
    } else {
        updateUnityWithAccounts(accounts);
    }
}

function updateUnityWithAccounts(accounts) {
    // Create a JSON object with address and all connected addresses
    const addressInfo = {
        address: accounts[0], // Active account
        addresses: accounts    // All accounts
    };

    // Convert the JSON object to a string
    const addressInfoString = JSON.stringify(addressInfo);

    // Pass the JSON string to Unity
    myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', addressInfoString);
}

export const createDataItemSigner = () => {
    if (connectedChain == 'arweave') {
      return async ({
        data,
        tags = [],
        target,
        anchor
      } = {}) => {
        // await checkArPermissions([
        //   'ACCESS_ADDRESS',
        //   'ACCESS_ALL_ADDRESSES',
        //   'ACCESS_PUBLIC_KEY',
        //   'SIGN_TRANSACTION',
        //   'SIGNATURE'
        // ])
  
        const signed = await window.arweaveWallet.signDataItem({
          data,
          tags,
          anchor,
          target
        })
        const dataItem = new DataItem(Buffer.from(signed))
  
        return {
          id: await dataItem.id,
          raw: await dataItem.getRaw()
        }
      }
    } else if (connectedChain == 'ethereum') {
      return async ({
        data,
        tags = [],
        target,
        anchor
      } = {}) => {
        const provider = new Web3Provider((window).ethereum)
        const signer = new InjectedEthereumSigner(provider)
        await signer.setPublicKey()
        const dataItem = createData(data, signer, { tags, target, anchor })
  
        await dataItem.sign(signer)
  
        return {
          id: dataItem.id,
          raw: dataItem.getRaw()
        }
      }
    } else {
      throw new Error(`Unsupported chain type: ${connectedChain}`)
    }
  }
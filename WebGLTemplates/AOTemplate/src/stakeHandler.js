import { Web3Provider } from '@ethersproject/providers';
import { ethers, formatEther, parseEther } from 'ethers';
import { ETH_CONTRACTS, Erc20_ABI, DaiBridge_ABI, StEthBridge_ABI } from './aoConfig';

// ---- Arweave address conversion helpers ----
function arweaveToBytes32(arAddr) {
    let b64 = arAddr.replace(/-/g, '+').replace(/_/g, '/');
    while (b64.length % 4) b64 += '=';
    const bin = atob(b64);
    const u8 = Uint8Array.from(bin, c => c.charCodeAt(0));
    return '0x' + Array.from(u8).map(b => b.toString(16).padStart(2, '0')).join('');
}

let provider = null
let signer = null

async function ensureConnected() {
    if (!window.ethereum) {
        throw new Error('No MetaMask wallet detected');
    }
    provider = provider || new Web3Provider(window.ethereum);
    // Prompt user to connect accounts if needed
    await provider.send('eth_requestAccounts', []);
    signer = provider.getSigner();
}

/**
 * Reads the user’s token balance (in decimal form).
 * @param {'dai'|'stEth'} token 
 */
export async function getTokenBalance(token) {
    try {
        await ensureConnected();
        const lc = token.toLowerCase();
        const tokenAddr = lc === 'dai'
            ? ETH_CONTRACTS.dai
            : ETH_CONTRACTS.stEth;
        const contract = new ethers.Contract(tokenAddr, Erc20_ABI, provider);
        const user = await signer.getAddress();
        const raw = await contract.balanceOf(user);            // BigNumber in wei
        const bal = formatEther(raw);             // e.g. "12.345678"

        myUnityInstance.SendMessage(
            'AOBridgeManager',
            'GetTokenBalanceCallback',
            JSON.stringify({ success: true, token: token, balance: bal })
        );
        return bal;
    } catch (error) {
        console.error('Error in getTokenBalance:', error);
        myUnityInstance.SendMessage(
            'AOBridgeManager',
            'GetTokenBalanceCallback',
            JSON.stringify({ success: false, token: token, error: error.message })
        );
        throw error;
    }
}

export async function getStakedBalance(token) {
    try {
        await ensureConnected();

        const tokenLower = token.toLowerCase();
        const bridgeAddress = tokenLower === 'dai' ? ETH_CONTRACTS.daiBridge : ETH_CONTRACTS.stEthBridge;
        const bridgeAbi = tokenLower === 'dai' ? DaiBridge_ABI : StEthBridge_ABI;
        const bridgeContract = new ethers.Contract(bridgeAddress, bridgeAbi, signer);

        const userAddress = await signer.getAddress();
        const userData = await bridgeContract.usersData(userAddress, 0);
        const depositedEther = formatEther(userData.deposited);

        myUnityInstance.SendMessage('AOBridgeManager', 'GetStakedBalanceCallback', JSON.stringify({ success: true, token: token, balance: depositedEther }));
    } catch (error) {
        console.error('Error in getStakedBalance:', error);
        myUnityInstance.SendMessage('AOBridgeManager', 'GetStakedBalanceCallback', JSON.stringify({ success: false, token: token, error: error.message }));
    }
}

export async function stake(token, amountEther, arweaveRecipient = null) {
    try {
        await ensureConnected();

        const tokenLower = token.toLowerCase();

        // Auto-approve ERC-20 if allowance is insufficient
        const tokenAddress = tokenLower === 'dai'
            ? ETH_CONTRACTS.dai
            : ETH_CONTRACTS.stEth;
        const bridgeAddress = tokenLower === 'dai' ? ETH_CONTRACTS.daiBridge : ETH_CONTRACTS.stEthBridge;
        const amount = parseEther(amountEther);
        const erc20Contract = new ethers.Contract(tokenAddress, Erc20_ABI, signer);
        const userAddr = await signer.getAddress();
        const allowance = await erc20Contract.allowance(userAddr, bridgeAddress);
        if (allowance < amount) {
            const responseApprove = await erc20Contract.approve(bridgeAddress, amount);
            const txApprove = await responseApprove.getTransaction();
            await txApprove.wait();
        }

        const bridgeAbi = tokenLower === 'dai' ? DaiBridge_ABI : StEthBridge_ABI;
        const bridgeContract = new ethers.Contract(bridgeAddress, bridgeAbi, signer);

        const recipientBytes32 = arweaveRecipient
            ? arweaveToBytes32(arweaveRecipient)
            : '0x' + '00'.repeat(32);

        const response = await bridgeContract.stake(0, amount, recipientBytes32);
        const tx = await response.getTransaction();
        const receipt = await tx.wait();

        console.log('Stake transaction receipt:', receipt);

        myUnityInstance.SendMessage('AOBridgeManager', 'StakeCallback', JSON.stringify({ success: true, token: token, balance: amountEther, txHash: receipt.transactionHash }));
    } catch (error) {
        console.error('Error in stake:', error);
        myUnityInstance.SendMessage('AOBridgeManager', 'StakeCallback', JSON.stringify({ success: false, token: token, error: error.message }));
    }
}

export async function unstake(token, amountEther, arweaveRecipient = null) {
    try {
        await ensureConnected();

        const tokenLower = token.toLowerCase();
        const bridgeAddress = tokenLower === 'dai' ? ETH_CONTRACTS.daiBridge : ETH_CONTRACTS.stEthBridge;
        const bridgeAbi = tokenLower === 'dai' ? DaiBridge_ABI : StEthBridge_ABI;
        const bridgeContract = new ethers.Contract(bridgeAddress, bridgeAbi, signer);

        const amount = parseEther(amountEther);
        const recipientBytes32 = arweaveRecipient
            ? arweaveToBytes32(arweaveRecipient)
            : '0x' + '00'.repeat(32);

        const response = await bridgeContract.withdraw(0, amount, recipientBytes32);
        const tx = await response.getTransaction();
        const receipt = await tx.wait();

        console.log('Unstake transaction receipt:', receipt);

        myUnityInstance.SendMessage('AOBridgeManager', 'UnstakeCallback', JSON.stringify({ success: true, token: token, balance: amountEther, txHash: receipt.transactionHash }));
    } catch (error) {
        console.error('Error in unstake:', error);
        myUnityInstance.SendMessage('AOBridgeManager', 'UnstakeCallback', JSON.stringify({ success: false, token: token, error: error.message }));
    }
}


import { Web3Provider } from '@ethersproject/providers';
import { ethers, formatEther, parseEther } from 'ethers';
import { ETH_CONTRACTS, Erc20_ABI, DaiBridge_ABI, StEthBridge_ABI, UsdsBridge_ABI } from './aoConfig';

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
 * @param {'dai'|'usds'|'stEth'} token 
 */
export async function getTokenBalance(token) {
    try {
        await ensureConnected();
        const lc = token.toLowerCase();
        const tokenAddr = lc === 'dai'
            ? ETH_CONTRACTS.dai
            : lc === 'usds'
            ? ETH_CONTRACTS.usds
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
        const bridgeAddress = tokenLower === 'dai' 
            ? ETH_CONTRACTS.daiBridge 
            : tokenLower === 'usds'
            ? ETH_CONTRACTS.usdsBridge
            : ETH_CONTRACTS.stEthBridge;
        const bridgeAbi = tokenLower === 'dai' 
            ? DaiBridge_ABI 
            : tokenLower === 'usds'
            ? UsdsBridge_ABI
            : StEthBridge_ABI;
        const bridgeContract = new ethers.Contract(bridgeAddress, bridgeAbi, signer);

        const userAddress = await signer.getAddress();
        
        // Note: USDS bridge has different structure - usersData(address) returns UserData struct
        // DAI and stETH use usersData(address, poolId)
        let depositedEther;
        if (tokenLower === 'usds') {
            const userData = await bridgeContract.usersData(userAddress);
            depositedEther = formatEther(userData.deposited);
        } else {
            const userData = await bridgeContract.usersData(userAddress, 0);
            depositedEther = formatEther(userData.deposited);
        }

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
            : tokenLower === 'usds'
            ? ETH_CONTRACTS.usds
            : ETH_CONTRACTS.stEth;
        const bridgeAddress = tokenLower === 'dai' 
            ? ETH_CONTRACTS.daiBridge 
            : tokenLower === 'usds'
            ? ETH_CONTRACTS.usdsBridge
            : ETH_CONTRACTS.stEthBridge;
        const amount = parseEther(amountEther);
        
        console.log(`💰 Staking ${amountEther} ${tokenLower.toUpperCase()}`);
        console.log(`   Token: ${tokenAddress}`);
        console.log(`   Bridge: ${bridgeAddress}`);
        console.log(`   Amount (wei): ${amount.toString()}`);
        
        const erc20Contract = new ethers.Contract(tokenAddress, Erc20_ABI, signer);
        const userAddr = await signer.getAddress();
        const allowance = await erc20Contract.allowance(userAddr, bridgeAddress);
        console.log(`   Allowance: ${formatEther(allowance)} ${tokenLower.toUpperCase()}`);
        
        if (allowance < amount) {
            console.log(`   Approving ${amountEther} ${tokenLower.toUpperCase()}...`);
            const responseApprove = await erc20Contract.approve(bridgeAddress, amount);
            const txApprove = await responseApprove.getTransaction();
            await txApprove.wait();
            console.log(`   ✅ Approval confirmed`);
        }

        const bridgeAbi = tokenLower === 'dai' 
            ? DaiBridge_ABI 
            : tokenLower === 'usds'
            ? UsdsBridge_ABI
            : StEthBridge_ABI;
        const bridgeContract = new ethers.Contract(bridgeAddress, bridgeAbi, signer);

        // Check minimal stake requirement for USDS
        if (tokenLower === 'usds') {
            try {
                const minimalStake = await bridgeContract.minimalStake();
                console.log(`   Minimal stake: ${formatEther(minimalStake)} USDS`);
                if (amount < minimalStake) {
                    throw new Error(`Amount ${amountEther} USDS is below minimal stake requirement of ${formatEther(minimalStake)} USDS`);
                }
            } catch (e) {
                console.warn('   Could not read minimal stake:', e.message);
            }
        }

        const recipientBytes32 = arweaveRecipient
            ? arweaveToBytes32(arweaveRecipient)
            : '0x' + '00'.repeat(32);
        
        console.log(`   Arweave recipient: ${arweaveRecipient || 'none'}`);
        console.log(`   Recipient bytes32: ${recipientBytes32}`);

        // Note: USDS bridge uses stake(amount, arweaveAddress) instead of stake(poolId, amount, arweaveAddress)
        console.log(`   Calling stake function...`);
        let response;
        if (tokenLower === 'usds') {
            response = await bridgeContract.stake(amount, recipientBytes32);
        } else {
            response = await bridgeContract.stake(0, amount, recipientBytes32);
        }
        console.log(`   Transaction hash: ${response.hash}`);
        console.log(`   Waiting for confirmation...`);
        
        const receipt = await response.wait();
        console.log('✅ Stake transaction confirmed:', receipt);

        myUnityInstance.SendMessage('AOBridgeManager', 'StakeCallback', JSON.stringify({ success: true, token: token, balance: amountEther, txHash: receipt.transactionHash }));
    } catch (error) {
        console.error('❌ Error in stake:', error);
        console.error('   Error details:', {
            message: error.message,
            code: error.code,
            reason: error.reason,
            transaction: error.transaction
        });
        myUnityInstance.SendMessage('AOBridgeManager', 'StakeCallback', JSON.stringify({ success: false, token: token, error: error.message, reason: error.reason }));
    }
}

export async function unstake(token, amountEther, arweaveRecipient = null) {
    try {
        await ensureConnected();

        const tokenLower = token.toLowerCase();
        const bridgeAddress = tokenLower === 'dai' 
            ? ETH_CONTRACTS.daiBridge 
            : tokenLower === 'usds'
            ? ETH_CONTRACTS.usdsBridge
            : ETH_CONTRACTS.stEthBridge;
        const bridgeAbi = tokenLower === 'dai' 
            ? DaiBridge_ABI 
            : tokenLower === 'usds'
            ? UsdsBridge_ABI
            : StEthBridge_ABI;
        const bridgeContract = new ethers.Contract(bridgeAddress, bridgeAbi, signer);

        const userAddress = await signer.getAddress();
        const amount = parseEther(amountEther);

        // Check withdrawal lock period for USDS
        if (tokenLower === 'usds') {
            try {
                const userData = await bridgeContract.usersData(userAddress);
                const lockPeriod = await bridgeContract.withdrawLockPeriodAfterStake();
                const currentTime = Math.floor(Date.now() / 1000);
                const unlockTime = Number(userData.lastStake) + Number(lockPeriod);
                
                if (currentTime < unlockTime) {
                    const remainingSeconds = unlockTime - currentTime;
                    const hours = Math.floor(remainingSeconds / 3600);
                    const minutes = Math.floor((remainingSeconds % 3600) / 60);
                    const timeMsg = hours > 0 ? `${hours}h ${minutes}m` : `${minutes} minutes`;
                    
                    throw new Error(`Withdrawal is locked. You can unstake in ${timeMsg}. (Lock period: ${Number(lockPeriod) / 86400} days after staking)`);
                }
            } catch (lockError) {
                // If it's our custom lock error, throw it
                if (lockError.message.includes('Withdrawal is locked')) {
                    throw lockError;
                }
                // Otherwise, log and continue (maybe contract doesn't have this function)
                console.warn('Could not check withdrawal lock period:', lockError.message);
            }
        }

        const recipientBytes32 = arweaveRecipient
            ? arweaveToBytes32(arweaveRecipient)
            : '0x' + '00'.repeat(32);

        // Note: USDS bridge uses withdraw(amount, arweaveAddress) instead of withdraw(poolId, amount, arweaveAddress)
        let response;
        if (tokenLower === 'usds') {
            response = await bridgeContract.withdraw(amount, recipientBytes32);
        } else {
            response = await bridgeContract.withdraw(0, amount, recipientBytes32);
        }
        const tx = await response.getTransaction();
        const receipt = await tx.wait();

        console.log('Unstake transaction receipt:', receipt);

        myUnityInstance.SendMessage('AOBridgeManager', 'UnstakeCallback', JSON.stringify({ success: true, token: token, balance: amountEther, txHash: receipt.transactionHash }));
    } catch (error) {
        console.error('Error in unstake:', error);
        myUnityInstance.SendMessage('AOBridgeManager', 'UnstakeCallback', JSON.stringify({ success: false, token: token, error: error.message }));
    }
}


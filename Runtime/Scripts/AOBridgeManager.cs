using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Permaverse.AO
{
    [System.Serializable]
    public class DelegationPreference
    {
        public string WalletFrom;
        public string WalletTo;
        public int Factor;
    }

    public class AOBridgeManager : MonoBehaviour
    {
        public static AOBridgeManager main { get; private set; }
        public string oracleAddress = "cuxSKjGJ-WDB9PzSkVkVVrIBSh3DrYHYz44usQOj5yE";
        public string stargridWallet = "";
        public AOConnectManager manager => AOConnectManager.main;
        public MessageHandler oracleMessageHandler;

        public Action<string> OnStakeCallback;
        public Action<string> OnUnstakeCallback;
        public Action<string> OnGetStakedBalanceCallback;
        public Action<string> OnGetTokenBalanceCallback;
        public Action<string> OnDelegationInfoReceivedCallback;

        [Space]

        public List<DelegationPreference> delegationPreferences = new List<DelegationPreference>();
        public long lastUpdate = 0;

        [DllImport("__Internal")]
        private static extern void StakeEvmJS(string token, string amount, string arweaveWallet);

        [DllImport("__Internal")]
        private static extern void UnstakeEvmJS(string token, string amount, string arweaveWallet);

        [DllImport("__Internal")]
        private static extern void GetStakedBalanceEvmJS(string token);

        [DllImport("__Internal")]
        private static extern void GetTokenBalanceEvmJS(string token);

        void Awake()
        {
            if (main == null)
            {
                main = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void GetDelegationInfo(string arweaveWallet)
        {
            // Use the new async method for better retry logic
            oracleMessageHandler.SendRequestAsync(oracleAddress, new List<Tag>
            {
                new Tag("Action","Get-Delegations"),
                new Tag("Wallet", arweaveWallet)
            }, callback: OnDelegationInfoReceived).Forget();
        }

        public void OnDelegationInfoReceived(bool result, NodeCU response)
        {
            // Clear previous data
            delegationPreferences.Clear();

            if (result && response.Messages.Count > 0)
            {
                string data = response.Messages[0].Data;
                if (data == null)
                {
                    Debug.LogError("No data in response");
                    return;
                }

                var json = JSON.Parse(data);
                if (json == null)
                {
                    Debug.LogError("Failed to parse JSON");
                    return;
                }

                // Get wallet from parent object
                string walletFrom = json["wallet"];
                lastUpdate = json["lastUpdate"].AsLong;

                // Handle the delegationPrefs array
                var delegationPrefsArray = json["delegationPrefs"];
                if (delegationPrefsArray == null || delegationPrefsArray.Count == 0)
                {
                    Debug.Log("No delegation preferences found");
                    return;
                }

                // Process all delegation preferences
                for (int i = 0; i < delegationPrefsArray.Count; i++)
                {
                    var delegationData = delegationPrefsArray[i];
                    if (delegationData == null) continue;

                    // Only require walletTo and factor fields
                    if (delegationData["walletTo"] != null && delegationData["factor"] != null)
                    {
                        DelegationPreference pref = new DelegationPreference
                        {
                            WalletFrom = walletFrom,
                            WalletTo = delegationData["walletTo"],
                            Factor = delegationData["factor"].AsInt
                        };

                        delegationPreferences.Add(pref);
                        Debug.Log($"Found delegation: {pref.WalletFrom} -> {pref.WalletTo} (factor: {pref.Factor})");
                    }
                }
            }
            else
            {
                Debug.LogError("Failed to get delegation info");
            }
        }

        public void ChangeDelegation(string walletFrom)
        {
            var data = new JSONObject();
            data["walletFrom"] = walletFrom;
            data["walletTo"] = stargridWallet;
            data["factor"] = 10000;

            // Use the new async method for better retry logic
            oracleMessageHandler.SendRequestAsync(oracleAddress, new List<Tag>
            {
                new Tag("Action","Set-Delegations"),
            }, data.ToString(), MessageHandler.NetworkMethod.Message, callback: OnDelegationChanged).Forget();
        }

        public void OnDelegationChanged(bool result, NodeCU response)
        {
            if (result)
            {
                Debug.Log("Delegation changed successfully");
            }
            else
            {
                Debug.LogError("Failed to change delegation");
            }
        }

        private void EnsureEvmWalletConnected(string expectedEvmId, Action onSuccess, Action<string> onError)
        {
            var connectManager = AOConnectManager.main;
            if (connectManager == null)
            {
                onError?.Invoke("{\"success\":false,\"error\":\"ConnectManager not found\"}");
                return;
            }

            if (string.IsNullOrEmpty(expectedEvmId))
            {
                onError?.Invoke("{\"success\":false,\"error\":\"No EVM ID provided\"}");
                return;
            }

            if (connectManager.IsWalletConnected(WalletType.EVM))
            {
                string connectedAddress = connectManager.GetSecondaryWalletInfo(WalletType.EVM)?.address;
                if (!string.Equals(connectedAddress, expectedEvmId, StringComparison.OrdinalIgnoreCase))
                {
                    onError?.Invoke("{\"success\":false,\"error\":\"EVM wallet address does not match user EVM ID\"}");
                    return;
                }
                onSuccess?.Invoke();
                return;
            }

            // Not connected: subscribe to OnWalletConnected, trigger connect, and check again on connect
            Action<WalletType> handler = null;
            handler = (walletType) =>
            {
                if (walletType == WalletType.EVM)
                {
                    connectManager.OnWalletConnected -= handler;
                    string connectedAddress = connectManager.GetSecondaryWalletInfo(WalletType.EVM)?.address;
                    if (!string.Equals(connectedAddress, expectedEvmId, StringComparison.OrdinalIgnoreCase))
                    {
                        onError?.Invoke("{\"success\":false,\"error\":\"EVM wallet address does not match user EVM ID\"}");
                        return;
                    }
                    onSuccess?.Invoke();
                }
            };
            connectManager.OnWalletConnected += handler;
            connectManager.ConnectWallet(WalletType.EVM);
        }

        public void StakeEVM(string token, string amount, string evmWalletId)
        {
            EnsureEvmWalletConnected(
                evmWalletId,
                () =>
                {
                    if (Application.isEditor)
                    {
                        Debug.Log($"Staking {amount} {token}");
                    }
                    else
                    {
                        StakeEvmJS(token, amount, stargridWallet);
                    }
                },
                (errorResult) =>
                {
                    Debug.LogError(errorResult);
                    OnStakeCallback?.Invoke(errorResult);
                }
            );
        }

        public void UnstakeEVM(string token, string amount, string evmWalletId)
        {
            EnsureEvmWalletConnected(
                evmWalletId,
                () =>
                {
                    if (Application.isEditor)
                    {
                        Debug.Log($"Unstaking {amount} {token}");
                    }
                    else
                    {
                        UnstakeEvmJS(token, amount, stargridWallet);
                    }
                },
                (errorResult) =>
                {
                    Debug.LogError(errorResult);
                    OnUnstakeCallback?.Invoke(errorResult);
                }
            );
        }

        public void GetStakedBalanceEVM(string token, string evmWalletId)
        {
            EnsureEvmWalletConnected(
                evmWalletId,
                () =>
                {
                    if (Application.isEditor)
                    {
                        Debug.Log($"Getting staked balance for {token}");
                    }
                    else
                    {
                        GetStakedBalanceEvmJS(token);
                    }
                },
                (errorResult) =>
                {
                    Debug.LogError(errorResult);
                    OnGetStakedBalanceCallback?.Invoke(errorResult);
                }
            );
        }

        public void GetTokenBalanceEVM(string token, string evmWalletId)
        {
            EnsureEvmWalletConnected(
                evmWalletId,
                () =>
                {
                    if (Application.isEditor)
                    {
                        Debug.Log($"Getting staked balance for {token}");
                    }
                    else
                    {
                        GetTokenBalanceEvmJS(token);
                    }
                },
                (errorResult) =>
                {
                    Debug.LogError(errorResult);
                    OnGetTokenBalanceCallback?.Invoke(errorResult);
                }
            );
        }

        public void StakeCallback(string result)
        {
            Debug.Log($"Stake Callback: {result}");
            OnStakeCallback?.Invoke(result);
        }

        public void UnstakeCallback(string result)
        {
            Debug.Log($"Unstake Callback: {result}");
            OnUnstakeCallback?.Invoke(result);
        }

        public void GetStakedBalanceCallback(string result)
        {
            Debug.Log($"GetStakedBalance Callback: {result}");
            OnGetStakedBalanceCallback?.Invoke(result);
        }

        public void GetTokenBalanceCallback(string result)
        {
            Debug.Log($"GetTokenBalance Callback: {result}");
            OnGetTokenBalanceCallback?.Invoke(result);
        }
    }
}